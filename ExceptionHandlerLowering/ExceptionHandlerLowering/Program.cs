﻿// Prototyping support for interpretation of TryExpression with a fault handler or exception filters.
//
// bartde - October 2015

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    class Program
    {
        static void Main(string[] args)
        {
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("bar"), AssemblyBuilderAccess.Save);
            var mod = asm.DefineDynamicModule("bar.dll");
            var typ = mod.DefineType("Foo");

            var lblRet = Expression.Label(typeof(int));
            var lbl1 = Expression.Label(typeof(int));
            var ex = Expression.Parameter(typeof(Exception));

            var expressions = new Expression[]
            {
                Expression.TryFault(
                    Expression.Constant(1),
                    Expression.Constant(2)
                ),
                Expression.Block(
                    Expression.TryFault(
                        Expression.Goto(lblRet, Expression.Constant(1)),
                        Expression.Constant(2)
                    ),
                    Expression.Label(lblRet, Expression.Constant(0))
                ),
                Expression.Block(
                    Expression.TryFault(
                        Expression.Block(
                            Expression.Goto(lblRet, Expression.Constant(1)),
                            Expression.Constant(2)
                        ),
                        Expression.Constant(3)
                    ),
                    Expression.Label(lblRet, Expression.Constant(0))
                ),
                Expression.Block(
                    Expression.TryFault(
                        Expression.Block(
                            Expression.Goto(lbl1, Expression.Constant(1)),
                            Expression.Goto(lblRet, Expression.Constant(2))
                        ),
                        Expression.Constant(3)
                    ),
                    Expression.Label(lbl1, Expression.Constant(-1)),
                    Expression.Label(lblRet, Expression.Constant(0))
                ),
                Expression.TryCatch(
                    Expression.Constant(1),
                    Expression.Catch(ex, Expression.Constant(2), Expression.Constant(true))
                ),
            };

            var i = 0;
            foreach (var expression in expressions)
            {
                var mtd = typ.DefineMethod("M" + i, MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);
                Expression.Lambda<Func<int>>(expression).CompileToMethod(mtd);

                var rewritten = new ExceptionHandlingLowering().Visit(expression);
                var cw = Expression.Lambda<Func<int>>(rewritten).Compile();
                cw();

                i++;
            }
        }
    }

    class ExceptionHandlingLowering : ExpressionVisitor
    {
        private static MethodInfo s_tryFault = typeof(ExceptionHandling).GetMethod("TryFault");
        private static MethodInfo s_tryFilter = typeof(ExceptionHandling).GetMethod("TryFilter");

        protected override Expression VisitTry(TryExpression node)
        {
            if (node.Fault != null)
            {
                return RewriteFaultHandler(node);
            }
            else if (node.Handlers.Any(h => h.Filter != null))
            {
                return RewriteFilterHandler(node);
            }

            return base.VisitTry(node);
        }

        private Expression RewriteFilterHandler(TryExpression node)
        {
            if (node.Handlers.Count == 1 && node.Finally == null)
            {
                //
                // 0. Recurse into the children.
                //
                var body = Visit(node.Body);
                var handler = VisitCatchBlock(node.Handlers[0]);

                //
                // 1. Initialize rewriter.
                //
                var rewriter = new HandlerRewriter();

                //
                // 2. Obtain rewriters for the body and the handler.
                //
                var bodyRewriter = rewriter.Rewrite(body);
                var catchRewriter = rewriter.Rewrite(handler.Body); // TODO: verify we can't jump out of the filter

                //
                // 3. Rewrite body, handler, and filter.
                //
                var rewrittenTry = bodyRewriter();
                var rewrittenTryHandler = Expression.Lambda<Func<LeaveHandlerData>>(rewrittenTry);
                var rewrittenCatch = catchRewriter();
                var rewrittenCatchHandler = Expression.Lambda(rewrittenTry, handler.Variable);
                var rewrittenFilterHandler = Expression.Lambda(handler.Filter, handler.Variable);

                //
                // 4. Emit call to TryFilter helper.
                //
                var tryFilter = Expression.Call(s_tryFilter.MakeGenericMethod(handler.Test), rewrittenTryHandler, rewrittenFilterHandler, rewrittenCatchHandler);

                //
                // 4. Create resulting expression.
                //
                return CreateRewrittenTry(node, rewriter, tryFilter);
            }
            else
            {
                return Visit(Expand(node));
            }
        }

        private Expression RewriteFaultHandler(TryExpression node)
        {
            //
            // 0. Recurse into the children.
            //
            var body = Visit(node.Body);
            var fault = Visit(node.Fault);

            //
            // 1. Initialize rewriter.
            //
            var rewriter = new HandlerRewriter();

            //
            // 2. Rewrite body and handler.
            //
            var rewrittenTry = rewriter.Rewrite(body)();
            var rewrittenTryHandler = Expression.Lambda<Func<LeaveHandlerData>>(rewrittenTry);
            var rewrittenFaultHandler = Expression.Lambda<Action>(fault);

            //
            // 3. Emit call to TryFault helper.
            //
            var tryFault = Expression.Call(s_tryFault, rewrittenTryHandler, rewrittenFaultHandler);

            //
            // 4. Create resulting expression.
            //
            return CreateRewrittenTry(node, rewriter, tryFault);
        }

        private Expression CreateRewrittenTry(TryExpression node, HandlerRewriter rewriter, Expression call)
        {
            var assignLeaveResult = Expression.Assign(rewriter.LeaveResult, call);
            var leaveResultValue = Expression.PropertyOrField(rewriter.LeaveResult, "Value");

            //
            // 1. Create jump table.
            //
            var dispatch = rewriter.GetDispatchTable();

            //
            // 2. Count number of expressions needed in resulting block.
            //
            var n = (node.Type == typeof(void) ? 0 : 1) + (dispatch != null ? 1 : 0) + 1;
            var statements = new Expression[n];

            //
            // 3. Store result of call in temporary.
            //
            var i = 0;
            statements[i++] = assignLeaveResult;

            //
            // 4. For non-emptyu dispatch tables, emit the dispatch.
            //
            if (dispatch != null)
            {
                statements[i++] = dispatch;
            }

            //
            // 5. For non-null nodes, extract the result.
            //
            if (node.Type != typeof(void))
            {
                statements[i++] = Expression.Convert(leaveResultValue, node.Type);
            }

            //
            // 6. Put everything together in a block.
            //
            var res = Expression.Block(node.Type, new[] { rewriter.LeaveResult }, statements);
            return res;
        }

        private Expression Expand(TryExpression expression)
        {
            var res = expression.Body;

            foreach (var handler in expression.Handlers)
            {
                res = Expression.TryCatch(res, handler);
            }

            if (expression.Finally != null)
            {
                res = Expression.TryFinally(res, expression.Finally);
            }

            return res;
        }

        class HandlerRewriter
        {
            private readonly ParameterExpression _leaveResult;
            private readonly LabelScanner _labelScanner;
            private readonly List<SwitchCase> _jumpTable;

            public HandlerRewriter()
            {
                _leaveResult = Expression.Parameter(typeof(LeaveHandlerData));
                _labelScanner = new LabelScanner(); // TODO: can we jump from try into catch?
                _jumpTable = new List<SwitchCase>();
            }

            public ParameterExpression LeaveResult
            {
                get { return _leaveResult; }
            }

            public Func<Expression> Rewrite(Expression body)
            {
                //
                // 0. Compute labels defined in the body.
                //
                _labelScanner.Visit(body);

                return () => RewriteHandler(body);
            }

            public SwitchExpression GetDispatchTable()
            {
                var leaveResultIndex = Expression.PropertyOrField(_leaveResult, "Index");

                var dispatch = default(SwitchExpression);

                if (_jumpTable.Count > 0)
                {
                    dispatch = Expression.Switch(
                        leaveResultIndex,
                        _jumpTable.ToArray()
                    );
                }

                return dispatch;
            }

            private Expression RewriteHandler(Expression body)
            {
                //
                // 1. Get accessors to the resulting Value and Index.
                //
                var leaveResultValue = Expression.PropertyOrField(_leaveResult, "Value");

                //
                // 2. Compute gotos that leave the body.
                //
                var gotoScanner = new GotoScanner(_labelScanner.Labels);
                gotoScanner.Visit(body);

                //
                // 3. Rewrite body.
                //
                var leaveLabel = Expression.Label(typeof(LeaveHandlerData));
                var rewriter = new Rewriter(leaveLabel, gotoScanner.LeaveLabels);
                var newBody = rewriter.Rewrite(body);

                //
                // 4. Create dispatch table.
                //
                if (gotoScanner.LeaveLabels.Count > 0)
                {
                    _jumpTable.AddRange(gotoScanner.LeaveLabels.Select(kv =>
                    {
                        var index = Expression.Constant(kv.Value);
                        var label = kv.Key;
                        var value = label.Type == typeof(void) ? null : Expression.Convert(leaveResultValue, label.Type);
                        var jump = Expression.Goto(label, value);
                        return Expression.SwitchCase(jump, index);
                    }));
                }

                return newBody;
            }

            class LabelScanner : ExpressionVisitor
            {
                public readonly HashSet<LabelTarget> Labels = new HashSet<LabelTarget>();

                protected override Expression VisitLabel(LabelExpression node)
                {
                    Labels.Add(node.Target);

                    return base.VisitLabel(node);
                }

                protected override Expression VisitLoop(LoopExpression node)
                {
                    if (node.BreakLabel != null)
                    {
                        Labels.Add(node.BreakLabel);
                    }

                    if (node.ContinueLabel != null)
                    {
                        Labels.Add(node.ContinueLabel);
                    }

                    return base.VisitLoop(node);
                }
            }

            class GotoScanner : ExpressionVisitor
            {
                private readonly HashSet<LabelTarget> _labels;
                public readonly IDictionary<LabelTarget, int> LeaveLabels = new Dictionary<LabelTarget, int>();

                public GotoScanner(HashSet<LabelTarget> labels)
                {
                    _labels = labels;
                }

                protected override Expression VisitGoto(GotoExpression node)
                {
                    if (!_labels.Contains(node.Target))
                    {
                        if (!LeaveLabels.ContainsKey(node.Target))
                        {
                            LeaveLabels.Add(node.Target, LeaveLabels.Count + 1);
                        }
                    }

                    return base.VisitGoto(node);
                }
            }

            class Rewriter : ExpressionVisitor
            {
                private static readonly ConstructorInfo s_leaveCtor = typeof(LeaveHandlerData).GetConstructor(new[] { typeof(int), typeof(object) });

                private readonly LabelTarget _leaveLabel;
                private readonly IDictionary<LabelTarget, int> _leaveLabels;

                public Rewriter(LabelTarget leaveLabel, IDictionary<LabelTarget, int> leaveLabels)
                {
                    _leaveLabel = leaveLabel;
                    _leaveLabels = leaveLabels;
                }

                public Expression Rewrite(Expression expression)
                {
                    var body = Visit(expression);

                    if (body.Type == typeof(void))
                    {
                        return
                            Expression.Block(
                                body,
                                Expression.Label(_leaveLabel, Expression.New(s_leaveCtor, Expression.Constant(0), Expression.Constant(null)))
                            );
                    }
                    else
                    {
                        var res = Expression.Parameter(body.Type);

                        return
                            Expression.Block(
                                new[] { res },
                                Expression.Assign(res, body),
                                Expression.Label(_leaveLabel, Expression.New(s_leaveCtor, Expression.Constant(0), Expression.Convert(res, typeof(object))))
                            );
                    }
                }

                protected override Expression VisitGoto(GotoExpression node)
                {
                    var value = Visit(node.Value);

                    var jumpIndex = default(int);
                    if (_leaveLabels.TryGetValue(node.Target, out jumpIndex))
                    {
                        var type = typeof(void);

                        if (node.Value != null)
                        {
                            type = node.Value.Type;
                        }

                        var res = type == typeof(void) ? (Expression)Expression.Constant(null) : Expression.Convert(value, typeof(object));
                        return Expression.Goto(_leaveLabel, Expression.New(s_leaveCtor, Expression.Constant(jumpIndex), res));
                    }

                    return base.VisitGoto(node);
                }
            }
        }
    }
}

namespace System.Runtime.CompilerServices
{
    public static class ExceptionHandling
    {
        public static LeaveHandlerData TryFilter<TException>(Func<LeaveHandlerData> body, Func<TException, bool> filter, Func<TException, LeaveHandlerData> handler)
            where TException : Exception
        {
            try
            {
                return body();
            }
            catch (TException ex) when (filter(ex))
            {
                return handler(ex);
            }
        }

        public static LeaveHandlerData TryFault(Func<LeaveHandlerData> body, Action handler)
        {
            var result = default(LeaveHandlerData);
            var success = false;
            try
            {
                result = body();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    handler();
                }
            }

            return result;
        }
    }

    public struct LeaveHandlerData
    {
        public LeaveHandlerData(int index, object value)
        {
            Index = index;
            Value = value;
        }

        public readonly int Index;
        public readonly object Value;
    }
}