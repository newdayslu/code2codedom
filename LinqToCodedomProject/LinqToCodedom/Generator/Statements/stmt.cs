﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToCodedom.Visitors;
using System.CodeDom;
using System.Linq.Expressions;

namespace LinqToCodedom.Generator
{
    public static partial class Builder
    {
        public static CodeStatement stmt(Expression<Action> exp)
        {
            return new CodeStatementVisitor(new VisitorContext()).Visit(exp);
        }

        public static CodeStatement stmt<T>(Expression<Action<T>> exp)
        {
            return new CodeStatementVisitor(new VisitorContext()).Visit(exp);
        }

        public static CodeStatement stmt<TResult>(Expression<Func<TResult>> exp)
        {
            return new CodeStatementVisitor(new VisitorContext()).Visit(exp);
        }

        public static CodeStatement stmt<TResult, T>(Expression<Func<TResult, T>> exp)
        {
            return new CodeStatementVisitor(new VisitorContext()).Visit(exp);
        }
    }
}
