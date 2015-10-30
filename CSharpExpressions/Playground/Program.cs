﻿// Prototyping extended expression trees for C#.
//
// bartde - October 2015

using Microsoft.CSharp.Expressions;
using System;
using System.Linq.Expressions;
using System.Reflection;
using static Playground.ReflectionUtils;

namespace Playground
{
    class Program
    {
        private static readonly MethodInfo s_writeLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });

        static void Main()
        {
            var xs = new int[2, 2, 2] { { { 0, 1 }, { 2, 3 } }, { { 4, 5 }, { 6, 7 } } };
            Console.WriteLine(xs.Length);
            return;

            for (var i = 0; i < xs.Rank; i++)
                Console.WriteLine(xs.GetLength(0));

            for (var i = 0; i < 2; i++)
                for (var j = 0; j < 2; j++)
                    for (var k = 0; k < 2; k++)
                        Console.WriteLine(xs.GetValue(i, j, k));

            return;

            Call();
            Invoke();
            New();
            Index();
        }

        static void Call()
        {
            Call1();
            Call2();
            Call3();
            Call4();
            Call5();
            Call6();
        }

        static void Call1()
        {
            var mtd = MethodInfoOf(() => Math.Min(default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));

            var call = CSharpExpression.Call(mtd, arg1, arg0);

            var res = Expression.Lambda<Func<int>>(call).Compile()();

            Console.WriteLine(res);
        }

        static void Call2()
        {
            var mtd = MethodInfoOf(() => Math.Min(default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));

            var call = CSharpExpression.Call(mtd, arg0, arg1);

            var res = Expression.Lambda<Func<int>>(call).Compile()();

            Console.WriteLine(res);
        }

        static void Call3()
        {
            var mtd = MethodInfoOf(() => F(default(int), default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));

            var call = CSharpExpression.Call(mtd, arg1, arg0);

            var res = Expression.Lambda<Func<int>>(call).Compile()();

            Console.WriteLine(res);
        }

        static void Call4()
        {
            var mtd = MethodInfoOf(() => F(default(int), default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));

            var call = CSharpExpression.Call(mtd, arg0, arg1);

            var res = Expression.Lambda<Func<int>>(call).Compile()();

            Console.WriteLine(res);
        }

        static void Call5()
        {
            var mtd = MethodInfoOf(() => F(default(int), default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];
            var val3 = mtd.GetParameters()[2];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));
            var arg2 = CSharpExpression.Bind(val3, Log(Expression.Constant(3), "C"));

            var call = CSharpExpression.Call(mtd, arg2, arg0, arg1);

            var res = Expression.Lambda<Func<int>>(call).Compile()();

            Console.WriteLine(res);
        }

        static void Call6()
        {
            var mtd = MethodInfoOf((Bar b) => b.F(default(int), default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];
            var val3 = mtd.GetParameters()[2];

            var obj = Log(Expression.Constant(new Bar()), "O");
            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));
            var arg2 = CSharpExpression.Bind(val3, Log(Expression.Constant(3), "C"));

            var call = CSharpExpression.Call(obj, mtd, arg2, arg0, arg1);

            var res = Expression.Lambda<Func<int>>(call).Compile()();

            Console.WriteLine(res);
        }

        static void Invoke()
        {
            Invoke1();
            Invoke2();
        }

        static void Invoke1()
        {
            var f = new Func<int, int, int>((a, b) => a + b);
            var mtd = MethodInfoOf(() => f.Invoke(default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];

            var obj = Log(Expression.Constant(f), "O");
            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));

            var invoke = CSharpExpression.Invoke(obj, arg1, arg0);

            var res = Expression.Lambda<Func<int>>(invoke).Compile()();

            Console.WriteLine(res);
        }

        static void Invoke2()
        {
            var f = new Func<int, int, int>((a, b) => a + b);
            var mtd = MethodInfoOf(() => f.Invoke(default(int), default(int)));

            var val1 = mtd.GetParameters()[0];
            var val2 = mtd.GetParameters()[1];

            var obj = Log(Expression.Constant(f), "O");
            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));

            var invoke = CSharpExpression.Invoke(obj, arg0, arg1);

            var res = Expression.Lambda<Func<int>>(invoke).Compile()();

            Console.WriteLine(res);
        }

        static void New()
        {
            New1();
            New2();
        }

        static void New1()
        {
            var ctor = ConstructorInfoOf(() => new TimeSpan(default(int), default(int), default(int)));

            var val1 = ctor.GetParameters()[0];
            var val2 = ctor.GetParameters()[1];
            var val3 = ctor.GetParameters()[2];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));
            var arg2 = CSharpExpression.Bind(val3, Log(Expression.Constant(3), "C"));

            var @new = CSharpExpression.New(ctor, arg2, arg0, arg1);

            var res = Expression.Lambda<Func<TimeSpan>>(@new).Compile()();

            Console.WriteLine(res);
        }

        static void New2()
        {
            var ctor = ConstructorInfoOf(() => new TimeSpan(default(int), default(int), default(int)));

            var val1 = ctor.GetParameters()[0];
            var val2 = ctor.GetParameters()[1];
            var val3 = ctor.GetParameters()[2];

            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));
            var arg2 = CSharpExpression.Bind(val3, Log(Expression.Constant(3), "C"));

            var @new = CSharpExpression.New(ctor, arg0, arg1, arg2);

            var res = Expression.Lambda<Func<TimeSpan>>(@new).Compile()();

            Console.WriteLine(res);
        }

        static void Index()
        {
            Index1();
            Index2();
        }

        static void Index1()
        {
            var get = MethodInfoOf((Field f) => f[default(int), default(int), default(int)]);
            var idx = get.DeclaringType.GetProperty(get.Name.Substring("get_".Length));

            var val1 = get.GetParameters()[0];
            var val2 = get.GetParameters()[1];
            var val3 = get.GetParameters()[2];

            var obj = Log(Expression.Constant(new Field()), "O");
            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));
            var arg2 = CSharpExpression.Bind(val3, Log(Expression.Constant(3), "C"));

            var index = CSharpExpression.Index(obj, idx, arg2, arg0, arg1);

            var res = Expression.Lambda<Func<int>>(index).Compile()();

            Console.WriteLine(res);
        }

        static void Index2()
        {
            var get = MethodInfoOf((Field f) => f[default(int), default(int), default(int)]);
            var idx = get.DeclaringType.GetProperty(get.Name.Substring("get_".Length));

            var val1 = get.GetParameters()[0];
            var val2 = get.GetParameters()[1];
            var val3 = get.GetParameters()[2];

            var obj = Log(Expression.Constant(new Field()), "O");
            var arg0 = CSharpExpression.Bind(val1, Log(Expression.Constant(1), "A"));
            var arg1 = CSharpExpression.Bind(val2, Log(Expression.Constant(2), "B"));
            var arg2 = CSharpExpression.Bind(val3, Log(Expression.Constant(3), "C"));

            var index = CSharpExpression.Index(obj, idx, arg0, arg1, arg2);

            var res = Expression.Lambda<Func<int>>(index).Compile()();

            Console.WriteLine(res);
        }

        static int F(int x, int y, int z = 42)
        {
            return x * y - z;
        }

        static Expression Log(Expression expression, string log)
        {
            return Expression.Block(Expression.Call(s_writeLine, Expression.Constant(log, typeof(string))), expression);
        }
    }

    class Bar
    {
        public int F(int x, int y, int z = 42)
        {
            return x * y - z;
        }
    }

    class Field
    {
        public int this[int x, int y, int z]
        {
            get
            {
                return x + y + z;
            }
        }
    }
}