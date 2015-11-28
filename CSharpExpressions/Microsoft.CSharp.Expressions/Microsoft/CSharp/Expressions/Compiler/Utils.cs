﻿// Prototyping extended expression trees for C#.
//
// bartde - October 2015

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Microsoft.CSharp.Expressions.Compiler
{
    static class Utils
    {
        public static Expression CreateRethrow(Expression err, Expression beforeThrow = null)
        {
            beforeThrow = beforeThrow ?? Expression.Empty();

            var exStronglyTyped = Expression.Parameter(typeof(Exception), "__exception");

            return
                Expression.Block(
                    typeof(void),
                    new[] { exStronglyTyped },
                    beforeThrow,
                    Expression.Assign(exStronglyTyped, Expression.TypeAs(err, typeof(Exception))),
                    Expression.IfThenElse(
                        Expression.ReferenceEqual(exStronglyTyped, Expression.Default(typeof(Exception))),
                        Expression.Throw(err), // NB: The C# compiler doesn't emit code to null out the hoisted local; maybe we should?
                        Expression.Call(
                            Expression.Call(
                                typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Capture), BindingFlags.Public | BindingFlags.Static),
                                exStronglyTyped
                            ),
                            typeof(ExceptionDispatchInfo).GetMethod(nameof(ExceptionDispatchInfo.Throw), BindingFlags.Public | BindingFlags.Instance)
                        )
                    ),
                    Expression.Assign(err, Expression.Default(err.Type))
                );
        }
    }
}