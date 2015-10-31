﻿// Prototyping extended expression trees for C#.
//
// bartde - October 2015

using System;

namespace Microsoft.CSharp.Expressions
{
    // DESIGN: The goal is to model the C# notion of statements as void-returning expressions,
    //         which differs from the DLR statement node semantics (e.g. typing of loops). We
    //         can revisit this if some C# statements get expression semantics. Also, we won't
    //         add redundant nodes just yet (e.g. TryCatch); instead, we'll focus on the C#-
    //         specific statement constructs.

    /// <summary>
    /// Base class for C# statement nodes.
    /// </summary>
    public abstract class CSharpStatement : CSharpExpression
    {
        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public override Type Type { get; } = typeof(void);
    }
}
