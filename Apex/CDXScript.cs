using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
#pragma warning disable VSTHRD010

namespace Apex
{
  unsafe struct Script
  {
    public static Expression<Func<object, object[]>> Compile(Type @this, string code)
    {
      var n = code.Length; var mem = Marshal.AllocCoTaskMem(34 + (n << 1));
      try
      {
        ptr = (byte*)mem.ToPointer(); var s = (char*)ptr + 16; fixed (char* p = code) Native.memcpy(s, p, (void*)((n + 1) << 1));
        #region remove comments
        for (int i = 0, l = n - 1; i < l; i++)
        {
          var c = s[i]; if (c <= 32) { s[i] = ' '; continue; }
          if (c == '"' || c == '\'') { for (++i; i < n && s[i] != c; i++) if (s[i] == '\\') i++; continue; }
          if (c != '/') continue;
          if (s[i + 1] == '/') { for (; i < n && s[i] != '\n'; i++) s[i] = ' '; continue; }
          if (s[i + 1] == '*') { var t = i; for (; i < l && !(s[i] == '/' && s[i - 1] == '*'); i++) ; for (; t <= i; t++) s[t] = ' '; continue; }
        }
        #endregion
        Script a; a.s = s; a.n = n; a.trim(); *(Script*)ptr = a;
        stack = new Stack { Expression.Parameter(typeof(object), "this") }; stack.@this = Expression.Parameter(@this, "this");
        if (map != null) stack.dict = new Dictionary<ParameterExpression, int>();
        stack.npub = 1; a.Parse(null, null, null, 0x08 | 0x04);
        var t1 = a.Parse(null, null, null, 0x01);
        var t2 = Expression.Lambda<Func<object, object[]>>(t1, ".ctor", stack.Take(1)); return t2;
      }
      catch { var e = (Script*)ptr; LastError = ((int)(e->s - (char*)(ptr + 32)), e->n); throw; }
      finally { Marshal.FreeCoTaskMem(mem); stack = null; dbg = null; map = null; bps = null; }
    }
    public static (int i, int n) LastError { get; private set; }
    public override string ToString() => new string(s, 0, n);
    char* s; int n; static byte* ptr; static Stack stack;
    class Stack : List<ParameterExpression> { internal ParameterExpression @this; internal List<object> usings = new List<object>(); internal int nstats, nusings, npub, xpos; internal List<Expression> list = new List<Expression>(); internal Dictionary<ParameterExpression, int> dict; internal string tuplnames; }
    Expression Parse(LabelTarget @return, LabelTarget @break, LabelTarget @continue, int flags)
    {
      var list = stack.list; int stackab = (flags & 1) != 0 ? 1 : stack.Count, listab = list.Count, ifunc = 0; var ep = *(Script*)ptr;
      if ((flags & 0x01) != 0)
      {
        stack.Add(stack.@this); list.Add(Expression.Assign(stack.@this, Expression.Convert(stack[0], stack.@this.Type)));
        if (dbg != null) { stack.xpos = stack.Count; stack.Add(Expression.Variable(typeof(Func<(int, object)[]>), "?")); stack.dict[stack[stack.xpos]] = -1; }
      }
      if (map != null && (flags & 0x02) != 0) { var s = this; for (s.n = 1; s.s > (char*)ptr + 16; s.s--) if (*s.s == '{') { __map(s); break; } }
      for (var c = this; c.n != 0;)
      {
        var a = c.block(); if (a.n == 0) continue;
        if (a.s[0] == '{') { if ((flags & 0x08) != 0) continue; a.trim(1, 1); list.Add(a.Parse(@return, @break, @continue, 0)); continue; }
        var t = a; var n = t.next(); *(Script*)ptr = a;
        if (n.equals("using"))
        {
          if ((flags & 0x08) == 0) continue;
          if (t.take("static")) { var u = GetType(t); stack.usings.Insert(stack.nstats++, u); }
          else
          {
            var ns = default(string);
            for (a = t; a.n != 0;)
            {
              var b = a.next('.'); if (ns != null) ns += '.'; ns += b.ToString();
              if (map != null) __map(b, a.n == 0 ? 0x88 : 0x48, ns);
            }
            stack.usings.Add(ns); //t.ToString().Replace(" ", string.Empty));
          }
          stack.nusings = stack.usings.Count; continue;
        }
        if (n.equals("if"))
        {
          if ((flags & 0x08) != 0) continue;
          n = t.next(); n.trim(1, 1);
          a = c; var l = c; for (; a.n != 0;) { var e = a.next(); if (!e.equals("else")) break; a.block(); l = a; }
          if (l.s != c.s) { a = c; c = l; a.next(); a.n = (int)(l.s - a.s); a.trim(); } else a.n = 0; __map(n);
          list.Add(Expression.IfThenElse(n.Parse(typeof(bool)), t.Parse(@return, @break, @continue, 0), a.n != 0 ? a.Parse(@return, @break, @continue, 0) : Expression.Empty())); continue;
        }
        if (n.equals("for"))
        {
          if ((flags & 0x08) != 0) continue;
          n = t.next(); n.trim(1, 1); var br = Expression.Label("break"); var co = Expression.Label("continue");
          a = n.next(';'); var t1 = stack.Count; var t2 = list.Count; a.Parse(null, null, null, 0x04);
          a = n.next(';'); var t3 = a.n != 0 ? a.Parse(typeof(bool)) : null;
          var t4 = list.Count; var t5 = stack.Count;
          t.Parse(@return, br, co, 0x04); list.Add(Expression.Label(co));
          //n.Parse(null, null, null, 0x04); 
          for (; n.n != 0;) { var v = n.next(','); v.Parse(null, null, null, 0x04); }
          var t6 = (Expression)Expression.Block(stack.Skip(t5), list.Skip(t4)); stack.RemoveRange(t5, stack.Count - t5); list.RemoveRange(t4, list.Count - t4);
          if (t3 != null) { var t7 = __dbg(a); if (t7 != null) t3 = Expression.Block(t7, t3); t6 = Expression.IfThenElse(t3, t6, Expression.Break(br)); }
          list.Add(Expression.Loop(t6, br)); t6 = Expression.Block(stack.Skip(t1), list.Skip(t2));
          stack.RemoveRange(t1, stack.Count - t1); list.RemoveRange(t2, list.Count - t2);
          list.Add(t6); continue;
        }
        if (n.equals("switch"))
        {
          if ((flags & 0x08) != 0) continue;
          n = t.next(); n.trim(1, 1); __map(n); var t1 = n.Parse(null);
          n = t.next(); n.trim(1, 1); var t2 = stack.usings.Count; var t5 = (Expression)null;
          for (var ab = list.Count; n.n != 0;)
          {
            for (; n.n != 0;)
            {
              t = n; a = t.next(); if (a.equals("default")) { n = t; n.next(); break; }
              if (!a.equals("case")) break; n = t; list.Add(Convert(n.next(':').Parse(t1.Type), t1.Type));
            }
            for (t = n; n.n != 0;)
            {
              a = n.block(); if (a.equals("break")) { t.n = (int)(a.s - t.s); t.trim(); break; }
              var s = a; s = s.next(); if (s.equals("return") || s.equals("continue")) { t.n = (int)(a.s - t.s) + a.n; t.trim(); break; }
            }
            var br = Expression.Label(); var t4 = t.Parse(@return, br, @continue, 0x10);
            if (list.Count != ab) stack.usings.Add(Expression.SwitchCase(t4, list.Skip(ab))); else t5 = t4;
            list.RemoveRange(ab, list.Count - ab);
          }
          list.Add(Expression.Switch(t1, t5, stack.usings.Skip(t2).Cast<SwitchCase>().ToArray()));
          stack.usings.RemoveRange(t2, stack.usings.Count - t2); continue;
        }
        if (n.equals("return"))
        {
          if ((flags & 0x08) != 0) continue;
          if (@return == null) n.error("syntax");
          if (@return.Type != typeof(void))
          {
            var t0 = @return.Type != typeof(Script) ? @return.Type : null;
            var t1 = t.Parse(t0); if (t0 == null) @return.GetType().GetField("_type", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(@return, t0 = t1.Type);
            n.n = (int)(t.s - n.s) + t.n; __map(n); list.Add(Expression.Return(@return, Convert(t1, t0))); continue;
          }
          if (t.n != 0) n.error("invalid type"); __map(n); list.Add(Expression.Return(@return)); continue;
        }
        if (n.equals("break")) { if ((flags & 0x08) != 0) continue; __map(n); list.Add(Expression.Break(@break)); continue; }
        if (n.equals("continue")) { if ((flags & 0x08) != 0) continue; __map(n); list.Add(Expression.Break(@continue)); continue; }
        if (n.equals("new")) { if ((flags & 0x08) != 0) continue; list.Add(a.Parse(null)); continue; }
        var @public = false; if (n.equals("public")) { a = t; @public = true; }
        n = a; t = n.gettype(); if (t.n == 0) { if ((flags & 0x08) != 0) continue; __map(a); list.Add(a.Parse(null)); continue; }
        a = n; n = a.next();
        var type = !t.equals("var") ? GetType(t) : null; var rettupl = stack.tuplnames;
        if (a.n != 0 && a.s[0] == '(')
        {
          if ((flags & 0x08) != 0) n.check(0);
          int i = 0; if ((flags & 0x01) != 0) for (i = 1; !n.stackequals(stack[i].Name); i++) ;
          var s = n.ToString(); var istack = i; if (i == 0) { istack = @public ? stack.npub++ : stack.Count; stack.Insert(istack, null); }
          t = a.next(); t.trim(1, 1); a.trim(1, 1); var ab = stack.Count;
          for (; t.n != 0;) { var u = t.next(','); var v = u.gettype(); u.check(ab); stack.Add(Expression.Parameter(GetType(v), u.ToString() + stack.tuplnames)); if (map != null && (flags & 0x08) == 0) __map(u, stack[stack.Count - 1], 0x05); }
          var t0 = i != 0 ? stack[istack].Type : type != typeof(void) ? Expression.GetFuncType(stack.Skip(ab).Select(p => p.Type).Concat(Enumerable.Repeat(type, 1)).ToArray()) : Expression.GetActionType(stack.Skip(ab).Select(p => p.Type).ToArray());
          var t1 = i != 0 ? stack[istack] : Expression.Variable(t0, s + rettupl); stack[istack] = t1;
          if ((flags & 0x08) != 0) { stack.RemoveRange(ab, stack.Count - ab); if (map != null) __map(n, t1, 0x03); continue; }
          var t2 = Expression.Lambda(t0, a.Parse(Expression.Label(type, "return"), null, null, 0x02), s + rettupl, stack.Skip(ab));
          stack.RemoveRange(ab, stack.Count - ab); list.Insert(++ifunc, Expression.Assign(t1, t2)); continue;
        }
        for (; n.n != 0; n = a.next())
        {
          var v = a.next(','); var b = v.next('='); if (!((flags & 0x01) != 0 && type != null)) n.check(stackab);
          if ((flags & 0x08) != 0) { if (type != null) { stack.Add(Expression.Parameter(type, n.ToString() + stack.tuplnames)); if (map != null) __map(n, stack[stack.Count - 1], 0x06); } continue; }
          stack.tuplnames = null; var r = type == null || v.n != 0 ? v.Parse(type) : null;
          int i = 0; if ((flags & 0x01) != 0 && type != null) for (i = 1; !n.stackequals(stack[i].Name); i++) ; var tupls = default(string);
          if (i == 0 && type == null) //xxx var 
          {
            var t1 = r; if (r is InvocationExpression u) t1 = u.Expression;
            if (t1 is ParameterExpression t2) { if (stack.tuplnames != null) tupls = stack.tuplnames; else { var x = t2.Name.IndexOf(','); if (x != -1) tupls = t2.Name.Substring(x); } }
            if (t1 is NewExpression) tupls = stack.tuplnames;
            //else { }
          }
          var e = i != 0 ? stack[i] : Expression.Parameter(type ?? r.Type, n.ToString() + tupls); //if (map != null) __map(n, 0x04, e.Type);
          if (r != null)
          {
            if (map != null) { var u = n; u.n = (int)(v.s - n.s) + v.n; __map(u); }
            list.Add(Expression.Assign(e, Convert(r, e.Type)));
          }
          if (i == 0) { if (dbg != null && (flags & 0x01) != 0) stack.Insert(stack.xpos++, e); else stack.Add(e); if (map != null) __map(n, e, (flags & 0x01) == 0 ? 0x04 : 0x06); }
        }
      }
      *(Script*)ptr = ep;
      if ((flags & 0x04) != 0) return null;
      if (map != null && (flags & 0x02) != 0) { var s = this; for (s = this, s.s += s.n, s.n = 1; *s.s != '}'; s.s++) ; __map(s); }
      if ((flags & 0x02) != 0) list.Add(@return.Type != typeof(void) ? Expression.Label(@return, Expression.Default(@return.Type)) : Expression.Label(@return));
      if ((flags & 0x01) != 0) { list.Add(Expression.NewArrayInit(typeof(object), stack.Take(stack.npub))); if (dbg != null) __dbg(); }
      if ((flags & 0x10) != 0) list.Add(Expression.Label(@break));
      var block = stack.Count != stackab || list.Count - listab > 1 ? Expression.Block(stack.Skip(stackab), list.Skip(listab)) : list.Count - listab == 1 ? list[listab] : Expression.Empty();
      list.RemoveRange(listab, list.Count - listab); stack.RemoveRange(stackab, stack.Count - stackab);
      return block;
    }
    Expression Parse(Type wt)
    {
      //(int x, int y) point; point = (1, 2); var e = point.x;
      if (n == 0) error("syntax");
      Script a = this, b = a, c, d; int op = 0, i1 = 0, i2 = 0;
      for (c = a; c.n != 0; i2++)
      {
        var t = c.next(); var o = t.opcode();
        if (o == 0) { i1 = 0; continue; }
        i1++; if (op >> 4 > o >> 4) continue;
        if (o >> 4 == 0x03 && (i2 == 0 || i1 != 1)) { i2--; continue; }
        if ((o == 0xd0 || o == 0xc0) && op == o) continue; // =, ?
        op = o; a.n = (int)(t.s - s); a.trim(); b = c;
      }
      if (op != 0)
      {
        if (op == 0xdf) // => 
        {
          if (wt == null) return Expression.Constant(this);
          var me = wt.GetMethod("Invoke"); if (me == null) error("unknown type");
          var pp = me.GetParameters(); if (a.s[0] == '(') a.trim(1, 1); var ab = stack.Count;
          int i = 0; for (; a.n != 0 && i < pp.Length; i++) { var n = a.next(','); stack.Add(Expression.Parameter(pp[i].ParameterType, n.ToString())); }
          if (a.n != 0 || i != pp.Length) error("invalid param count");
          var g = me.ReturnType.ContainsGenericParameters; Expression r;
          if (b.s[0] == '{') { b.trim(1, 1); r = b.Parse(Expression.Label(g ? typeof(Script) : me.ReturnType, "return"), null, null, 0x02); }
          else r = b.Parse(g ? null : wt);
          if (g) { if (r.Type == typeof(Script)) b.error("missing return"); var gg = wt.GetGenericArguments(); gg[gg.Length - 1] = r.Type; wt = wt.GetGenericTypeDefinition().MakeGenericType(gg); }
          r = Expression.Lambda(wt, r, stack.Skip(ab)); stack.RemoveRange(ab, stack.Count - ab); return r;
        }
        var ea = a.Parse(null); var wwt = default(Type);
        switch (op)
        {
          case 0x54: return Expression.TypeIs(ea, GetType(b));
          case 0x55: return Expression.TypeAs(ea, GetType(b));
          case 0xc0: { a = b.next(':'); var t1 = a.Parse(wt); var t2 = b.Parse(wt ?? t1.Type); return Expression.Condition(ea, t1, t2); }
          case 0x40: case 0x41: case 0xd9: case 0xda: wwt = typeof(int); break; //<< >>
        }
        var eb = b.Parse(wwt ?? ea.Type);
        if (op == 0x30 && (ea.Type == typeof(string) || eb.Type == typeof(string)))
        {
          if (ea.Type != typeof(string)) ea = Expression.Call(ea, ea.Type.GetMethod("ToString", Type.EmptyTypes));
          if (eb.Type != typeof(string)) eb = Expression.Call(eb, eb.Type.GetMethod("ToString", Type.EmptyTypes));
          return Expression.Call(typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }), ea, eb);
        }
        var sp = *(Script*)ptr; *(Script*)ptr = this; MethodInfo mo = null;
        switch (op)
        {
          case 0x20: case 0xd1: mo = refop("op_Multiply", ea, eb); break;
          case 0x21: case 0xd2: mo = refop("op_Division", ea, eb); break;
          case 0x22: case 0xd3: mo = refop("op_Modulus", ea, eb); break;
          case 0x30: case 0xd4: mo = refop("op_Addition", ea, eb); break;
          case 0x31: case 0xd5: mo = refop("op_Subtraction", ea, eb); break;
          case 0x70: case 0xd6: mo = refop("op_BitwiseAnd", ea, eb); break;
          case 0x80: case 0xd7: mo = refop("op_ExclusiveOr", ea, eb); break;
          case 0x90: case 0xd8: mo = refop("op_BitwiseOr", ea, eb); break;
          case 0x40: case 0xd9: mo = refop("op_LeftShift", ea, eb); break;
          case 0x41: case 0xda: mo = refop("op_RightShift", ea, eb); break;
          case 0x50: mo = refop("op_LessThan", ea, eb); break;
          case 0x51: mo = refop("op_GreaterThan", ea, eb); break;
          case 0x52: mo = refop("op_LessThanOrEqual", ea, eb); break;
          case 0x53: mo = refop("op_GreaterThanOrEqual", ea, eb); break;
          case 0x60: mo = refop("op_Equality", ea, eb); break;
          case 0x61: mo = refop("op_Inequality", ea, eb); break;
        }
        if (mo != null)
        {
          var pp = mo.GetParameters();
          //if (pp[1].ParameterType.IsByRef && eb is MemberExpression me && me.Member.DeclaringType.IsValueType) 
          //{
          //  var t = Expression.Variable(eb.Type, string.Empty); stack.Add(t); eb = Expression.Assign(t, eb);
          //}
          //if ((pp[1].Attributes & ParameterAttributes.In) != 0) { }
          ea = Convert(ea, pp[0].ParameterType);
          eb = Convert(eb, pp[1].ParameterType);
        }
        else if (ea.Type != eb.Type && wwt == null)
        {
          if (Convertible(eb.Type, ea.Type) != null) eb = Convert(eb, ea.Type);
          else if (Convertible(ea.Type, eb.Type) != null) ea = Convert(ea, eb.Type);
          else if ((op & 0xf0) == 0x60 && (ea.Type.IsEnum || eb.Type.IsEnum))
          {
            if (0.Equals((ea as ConstantExpression)?.Value)) ea = Expression.Convert(ea, eb.Type);
            if (0.Equals((eb as ConstantExpression)?.Value)) eb = Expression.Convert(eb, ea.Type);
          }
        }

        wt = op >= 0x70 && op <= 0x90 && ea.Type.IsEnum ? ea.Type : null; // | & ^
        if (wt != null)
        {
          var x = wt.GetEnumUnderlyingType();
          if (ea.Type != x) ea = Expression.Convert(ea, x);
          if (eb.Type != x) eb = Expression.Convert(eb, x);
        }
        switch (op)
        {
          case 0x20: eb = Expression.Multiply(ea, eb, mo); break;
          case 0x21: eb = Expression.Divide(ea, eb, mo); break;
          case 0x22: eb = Expression.Modulo(ea, eb, mo); break;
          case 0x30: eb = Expression.Add(ea, eb, mo); break;
          case 0x31: eb = Expression.Subtract(ea, eb, mo); break;
          case 0x40: eb = Expression.LeftShift(ea, eb, mo); break;
          case 0x41: eb = Expression.RightShift(ea, eb, mo); break;
          case 0x50: eb = Expression.LessThan(ea, eb, false, mo); break;
          case 0x51: eb = Expression.GreaterThan(ea, eb, false, mo); break;
          case 0x52: eb = Expression.LessThanOrEqual(ea, eb, false, mo); break;
          case 0x53: eb = Expression.GreaterThanOrEqual(ea, eb, false, mo); break;
          case 0x60: eb = Expression.Equal(ea, eb, false, mo); break;
          case 0x61: eb = Expression.NotEqual(ea, eb, false, mo); break;
          case 0x70: eb = Expression.And(ea, eb, mo); break;
          case 0x80: eb = Expression.ExclusiveOr(ea, eb, mo); break;
          case 0x90: eb = Expression.Or(ea, eb, mo); break;
          case 0xa0: eb = Expression.OrElse(ea, eb); break;
          case 0xb0: eb = Expression.AndAlso(ea, eb); break;
          case 0xc2: eb = Expression.Coalesce(ea, eb); break;
          case 0xd0: eb = Expression.Assign(ea, eb); break; // bugs in Expressions if (((MemberExpression)ea).Member.DeclaringType.IsValueType) ...
          case 0xd1: eb = Expression.Assign(ea, Expression.Multiply(ea, eb, mo)); break;//Expression.MultiplyAssign(ea, eb);
          case 0xd2: eb = Expression.Assign(ea, Expression.Divide(ea, eb, mo)); break; //Expression.DivideAssign(ea, eb);
          case 0xd3: eb = Expression.Assign(ea, Expression.Modulo(ea, eb, mo)); break;//return Expression.ModuloAssign(ea, eb);
          case 0xd4: eb = Expression.Assign(ea, Expression.Add(ea, eb, mo)); break; //return Expression.AddAssign(ea, eb);//if (((MemberExpression)ea).Member.DeclaringType.IsValueType)
          case 0xd5: eb = Expression.Assign(ea, Expression.Subtract(ea, eb, mo)); break; //return Expression.SubtractAssign(ea, eb);
          case 0xd6: eb = Expression.Assign(ea, Expression.And(ea, eb, mo)); break; //return Expression.AndAssign(ea, eb);
          case 0xd7: eb = Expression.Assign(ea, Expression.ExclusiveOr(ea, eb, mo)); break; //return Expression.ExclusiveOrAssign(ea, eb);
          case 0xd8: eb = Expression.Assign(ea, Expression.Or(ea, eb, mo)); break;//return Expression.OrAssign(ea, eb);
          case 0xd9: eb = Expression.Assign(ea, Expression.LeftShift(ea, eb)); break; //return Expression.LeftShiftAssign(ea, eb);
          case 0xda: eb = Expression.Assign(ea, Expression.RightShift(ea, eb)); break; //return Expression.RightShiftAssign(ea, eb);
        }
        if (wt != null) eb = Expression.Convert(eb, wt);
        *(Script*)ptr = sp; return eb;
      }
      b = this; a = b.next();
      if (a.n == 1)
      {
        if (a.s[0] == '+') { var t = b.Parse(wt); return t; } //"op_UnaryPlus"
        if (a.s[0] == '-') { var t = b.Parse(wt); return Expression.Negate(t, refop("op_UnaryNegation", t)); }
        if (a.s[0] == '!') { var t = b.Parse(wt); return Expression.Not(t, refop("op_LogicalNot", t)); }
        if (a.s[0] == '~') { var t = b.Parse(wt); return Expression.OnesComplement(t, refop("op_OnesComplement", t)); }
      }
      if (a.n == 2 && a.s[0] == '+' & a.s[1] == '+') return Expression.PreIncrementAssign(b.Parse(wt));
      if (a.n == 2 && a.s[0] == '-' & a.s[1] == '-') return Expression.PreDecrementAssign(b.Parse(wt));
      if (b.n == 2 && b.s[0] == '+' & b.s[1] == '+') return Expression.PostIncrementAssign(a.Parse(wt));
      if (b.n == 2 && b.s[0] == '-' & b.s[1] == '-') return Expression.PostDecrementAssign(a.Parse(wt));

      Expression left = null; Type type = null; bool checkthis = false, priv = false;
      if (a.s[0] == '(')
      {
        a.trim(1, 1);
        if (b.n != 0 && b.s[0] != '.' && b.s[0] != '[') { var t = b.Parse(wt = GetType(a)); return t.Type != wt ? Expression.Convert(t, wt) : t; }
        if (b.n == 0) //xxx
        {
          var l = 0; for (var u = a; u.n != 0; u.next(','), l++) ;
          if (l > 1 && l <= 8)
          {
            var u = a; ConstructorInfo ci = null; ParameterInfo[] pi = null; stack.tuplnames = null;
            if (wt != null)
            {
              var cc = wt.GetConstructors();
              for (int i = 0; i < cc.Length; i++) { var up = cc[i].GetParameters(); if (up.Length == l) { ci = cc[i]; pi = up; break; } }
            }
            var ee = new Expression[l];
            for (int i = 0; i < l; i++)
            {
              var v = u.next(','); var h = v.next(':'); if (v.n == 0) { var t = v; v = h; h = t; }
              if (h.n != 0 || stack.tuplnames != null) addtup(i, h); var t1 = stack.tuplnames;
              ee[i] = v.Parse(pi != null ? pi[i].ParameterType : null); stack.tuplnames = t1;
            }
            if (ci == null)
            {
              var tt = new Type[l]; for (int i = 0; i < l; i++) tt[i] = ee[i].Type;
              ci = getvtup(l).MakeGenericType(tt).GetConstructors()[0];
            }
            left = Expression.New(ci, ee); goto eval;
          }
        }
        left = a.Parse(wt); goto eval;
      }
      if (char.IsNumber(a.s[0]) || a.s[0] == '.')
      {
        if (a.n > 1 && a.s[0] == '0' && (a.s[1] | 0x20) == 'x')
        {
          a.trim(2, 0); if (wt == typeof(uint) || (a.n == 8 && a.s[0] > '7')) { left = Expression.Constant(uint.Parse(a.ToString(), NumberStyles.HexNumber)); goto eval; }
          left = Expression.Constant(a.n > 8 ? (object)long.Parse(a.ToString(), NumberStyles.HexNumber) :
            int.Parse(a.ToString(), NumberStyles.HexNumber)); goto eval;
        }
        var tc = TypeCode.Int32;
        switch (a.s[n - 1] | 0x20)
        {
          case 'f': a.trim(0, 1); tc = TypeCode.Single; break;
          case 'd': a.trim(0, 1); tc = TypeCode.Double; break;
          case 'm': a.trim(0, 1); tc = TypeCode.Decimal; break;
          case 'u': a.trim(0, 1); tc = TypeCode.UInt32; break;
          case 'l': a.trim(0, 1); tc = TypeCode.Int64; if ((a.s[n - 1] | 0x20) == 'u') { a.trim(0, 1); tc = TypeCode.UInt64; } break;
          default: for (int i = 0; i < a.n; i++) if (!char.IsNumber(a.s[i])) { tc = TypeCode.Double; break; } break;
        }
        if (wt != null)
        {
          var v = Type.GetTypeCode(wt);
          if (v != tc)
            if (v > tc) tc = v;
            else if (tc < TypeCode.Single && v > TypeCode.Char && v < TypeCode.Single) tc = v;
        }
        left = Expression.Constant(System.Convert.ChangeType(a.ToString(), tc, CultureInfo.InvariantCulture));
        if (wt != null && wt.IsEnum) if (0.Equals(((ConstantExpression)left).Value)) left = Expression.Convert(left, wt);
        goto eval;
      }
      if (a.s[0] == '"') { var ss = new string(a.s, 1, a.n - 2); if (ss.IndexOf('\\') >= 0) ss = Regex.Unescape(ss); left = Expression.Constant(ss); goto eval; }
      if (a.s[0] == '\'') { if (a.n == 3) { left = Expression.Constant(a.s[1]); goto eval; } var ss = Regex.Unescape(new string(a.s, 1, a.n - 2)); if (ss.Length != 1) a.error("syntax"); left = Expression.Constant(ss[0]); goto eval; }
      if (a.equals("true")) { left = Expression.Constant(true); goto eval; }
      if (a.equals("false")) { left = Expression.Constant(false); goto eval; }
      if (a.equals("null")) { left = Expression.Constant(null, wt ?? typeof(object)); goto eval; }
      if (a.equals("typeof")) { a = b.next(); a.trim(1, 1); left = Expression.Constant(GetType(a)); goto eval; }
      if (a.equals("new"))
      {
        for (a = b; b.n != 0 && b.s[0] != '(' && b.s[0] != '[' && b.s[0] != '{'; b.next()) ;
        a.n = (int)(b.s - a.s); a.trim(); var t = GetType(a); a = b.next(); var tc = a.s[0]; a.trim(1, 1);
        if (tc == '[') while (b.n != 0 && b.s[0] == '[') { t = t.MakeArrayType(); b.next(); }
        var ab = stack.list.Count; if (tc != '{') for (; a.n != 0;) stack.list.Add(a.next(',').Parse(typeof(int)));
        if (tc == '[')
        {
          if (ab != stack.list.Count) left = Expression.NewArrayBounds(t, stack.list.Skip(ab));
          else
          {
            a = b.next(); if (a.n == 0 || a.s[0] != '{') a.error("syntax");
            for (a.trim(1, 1); a.n != 0;) stack.list.Add(Convert(a.next(',').Parse(t), t));
            left = Expression.NewArrayInit(t, stack.list.Skip(ab));
          }
        }
        else
        {
          if (ab == stack.list.Count) left = Expression.New(t);
          else
          {
            var ct = GetMember(t, null, BindingFlags.Instance | BindingFlags.Public, ab, stack.usings.Count) as ConstructorInfo;
            if (ct == null) error("invalid ctor");
            left = Expression.New(ct, stack.list.Skip(ab));
          }
          if (b.n != 0 && b.s[0] == '{') { a = b.next(); a.trim(1, 1); tc = '{'; }
          if (tc == '{')
          {
            var ic = left.Type.GetInterface("ICollection`1"); var ns = stack.list.Count;
            if (ic != null)
            {
              for (t = ic.GetGenericArguments()[0]; a.n != 0;) stack.list.Add(Convert(a.next(',').Parse(t), t));
              left = Expression.ListInit((NewExpression)left, stack.list.Skip(ns));
            }
            else
            {
              var pp = stack.usings; var np = pp.Count;//var list = new List<MemberAssignment>();
              for (; a.n != 0;) { var p = a.next(','); var e = Expression.PropertyOrField(left, p.next('=').ToString()); pp.Add(Expression.Bind(e.Member, Convert(p.Parse(e.Type), e.Type))); }
              left = Expression.MemberInit((NewExpression)left, pp.Skip(np).Cast<MemberAssignment>()); pp.RemoveRange(np, pp.Count - np);
            }
          }
        }
        stack.list.RemoveRange(ab, stack.list.Count - ab); goto eval;
      }
      for (int i = stack.Count - 1; i >= 0; i--) if (a.stackequals(stack[i].Name)) { left = stack[i]; if (map != null) __map(a, stack[i], 0); goto eval; }
      var sa = a.ToString();
      for (int i = stack.nstats; --i >= 0;)
      {
        var fi = ((Type)stack.usings[i]).GetMember(sa, MemberTypes.Property | MemberTypes.Field, BindingFlags.Static | BindingFlags.Public);
        if (fi.Length != 0) { left = Expression.MakeMemberAccess(left, fi[0]); if (map != null) __map(a, 0x01, fi[0]); goto eval; }
      }
      for (d = a, c = b; c.n != 0 && (c.s[0] == '.' || c.s[0] == '·');)
      {
        if ((type = GetType(d, false)) != null) { b = c; goto eval; }
        c.next(); c.next(); d = a; d.n = (int)(c.s - d.s); d.trim();
      }
      left = stack.@this; checkthis = true; eval:
      for (; b.n != 0 || checkthis; type = null, checkthis = priv = false)
      {
        if (checkthis) goto call; a = b.next();
        if (a.s[0] == '[')
        {
          a.trim(1, 1); var ab = stack.list.Count; var ar = left.Type.IsArray; //var xx = new int[6][];
          for (; a.n != 0;) { var t = a.next(','); stack.list.Add(ar ? Convert(t.Parse(typeof(int)), typeof(int)) : t.Parse(null)); }
          left = ar ? Expression.ArrayAccess(left, stack.list.Skip(ab)) : Expression.Property(left, left.Type == typeof(string) ? "Chars" : "Item", stack.list.Skip(ab).ToArray());
          stack.list.RemoveRange(ab, stack.list.Count - ab); continue;
        }
        if (a.s[0] == '(')
        {
          a.trim(1, 1); var ab = stack.list.Count; for (; a.n != 0;) stack.list.Add(a.next(',').Parse(null));
          var pb = left.Type.GetMethod("Invoke").GetParameters(); if (pb.Length != stack.list.Count - ab) a.error("invalid param count");
          for (int i = 0; i < pb.Length; i++) stack.list[ab + i] = Convert(stack.list[ab + i], pb[i].ParameterType);
          left = Expression.Invoke(left, stack.list.Skip(ab)); stack.list.RemoveRange(ab, stack.list.Count - ab); continue;
        }
        if (a.n != 1 || !(a.s[0] == '.' || (priv = a.s[0] == '·'))) a.error("syntax");
        a = b.next(); call:
        var bf = (type != null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public;
        var t1 = type ?? left.Type; if (priv || t1 == stack.@this.Type) bf |= BindingFlags.NonPublic | BindingFlags.FlattenHierarchy; // BindingFlags.NonPublic;
        var s = a.ToString();
        if (b.n != 0 && (b.s[0] == '(' || b.s[0] == '<'))
        {
          var ng = stack.usings.Count;
          if (b.s[0] == '<') { c = b.next(); c.trim(1, 1); for (; c.n != 0;) stack.usings.Add(GetType(c.next(','))); }
          c = b.next(); if (c.n > 1 && c.s[c.n - 1] == ')') c.trim(1, 1); else c.n = 0; var ab = stack.list.Count;
          for (; c.n != 0;) { var t = c.next(','); if (t.take("ref") || t.take("out")) { } stack.list.Add(t.Parse(null)); }
          var me = GetMember(t1, s, bf, ab, ng);
          if (me == null && checkthis)
          {
            left = null;
            for (int i = stack.nstats; --i >= 0;) if ((me = GetMember((Type)stack.usings[i], s, BindingFlags.Static | BindingFlags.Public, ab, ng)) != null) break;
          }
          if (me == null && left != null)
          {
            stack.list.Insert(ab, left); left = null; GetExtensions();
            for (int i = 0; i < extensions.Length; i++) if ((me = GetMember(extensions[i], s, BindingFlags.Static | BindingFlags.Public, ab, ng)) != null) break; ;
          }
          if (map != null) { var xm = me; if (xm == null) xm = GetMember(t1, s, bf, ab, -1); if (xm != null) __map(a, 0, xm); }
          if (me == null) { a.n = (int)(b.s - a.s); a.trim(); a.error("unknown method" + " " + a.ToString()); }
          left = Expression.Call(left, (MethodInfo)me, stack.list.Skip(ab)); stack.list.RemoveRange(ab, stack.list.Count - ab); stack.usings.RemoveRange(ng, stack.usings.Count - ng);
          if (type == typeof(Debug) && !Debugger.IsAttached && (me.GetCustomAttribute(typeof(ConditionalAttribute)) as ConditionalAttribute)?.ConditionString == "DEBUG") left = Expression.Empty();
          continue;
        }

        var x = t1.GetMember(s, MemberTypes.Property | MemberTypes.Field, bf);
        if (x.Length != 0) { left = Expression.MakeMemberAccess(left, x[0]); if (map != null) __map(a, 0x01, x[0]); continue; }
        if ((bf & BindingFlags.Static) != 0) { type = t1.GetNestedType(s, bf); if (type != null) goto eval; }
        if (left is ParameterExpression pe) //xxx 
        {
          var ss = pe.Name.Split(','); var i = Array.IndexOf(ss, s, 1);
          if (i >= 1) { var f = t1.GetFields()[i - 1]; left = Expression.Field(left, f); if (map != null) __map(a, 0x01, f); continue; }
        }
        left = null; break;
      }
      if (left == null || checkthis) a.error();
      return left;
    }
    static MethodInfo refop(string op, Expression a)
    {
      Type t1 = a.Type; if (t1.IsPrimitive) return null;
      var me = t1.GetMethod(op, BindingFlags.Static | BindingFlags.Public, null, new Type[] { t1.MakeByRefType() }, null); if (me != null) return me;
      return null;
    }
    static MethodInfo refop(string op, Expression a, Expression b)
    {
      Type t1 = a.Type, t2 = b.Type; if (t1 == t2 && t1.IsPrimitive) return null;
      MethodInfo match = null;
      for (int i = 0; i < 2; i++)
      {
        var mm = (i == 0 ? t1 : t2).GetMember(op, MemberTypes.Method, BindingFlags.Static | BindingFlags.Public);
        for (int k = 0; k < mm.Length; k++)
        {
          var m = (MethodInfo)mm[k]; var pp = m.GetParameters();
          if (pp[0].ParameterType == t1 && pp[1].ParameterType == t2) return m;
          if (Convertible(t1, pp[0].ParameterType) == null) continue;
          if (Convertible(t2, pp[1].ParameterType) == null) continue;
          match = m;
        }
        if (t1 == t2) break;
      }
      return match;
    }
    static Type GetType(Script a, bool ex = true)
    {
      stack.tuplnames = null;
      if (a.s[0] == '(') //xxx (ushort[] ii, int ni)
      {
        var u = a; u.trim(1, 1); int c = 0; for (var v = u; v.n != 0; v.next(','), c++) ;
        if (c > 1 && c <= 8)
        {
          var tt = new Type[c]; //var ss = default(string[]);// new string[c];
          for (c = 0; c < tt.Length; c++)
          {
            var t1 = stack.tuplnames;
            var v = u.next(','); var w = v.gettype(); tt[c] = GetType(w.n != 0 ? w : v, ex);
            stack.tuplnames = t1; if (w.n != 0 || stack.tuplnames != null) addtup(c, v); //v:name
          }
          return getvtup(tt.Length).MakeGenericType(tt);
        }
      }
      if (map != null)
      {
        var t1 = map; map = null; Type t2 = null; try { t2 = GetType(a, ex); }
        finally
        {
          map = t1;
          var ns = default(string);
          for (var e = a; e.n != 0;)
          {
            var c = e.next('.'); if (e.n == 0 && t2 != null)
            {
              if (c.s[0] != '(') //xxx
                c = c.next(); __map(c, 0, t2); break;
            }
            for (var t3 = t2; t3 != null && t3.IsNested;) { t3 = t3.DeclaringType; if (c.equals(t3.Name)) { __map(c, 0, t3); break; } }
            if (ns != null) ns += '.'; ns += c.ToString(); __map(c, 0x08, ns); //ns
          }
        }
        return t2;
      }
      if (a.s[a.n - 1] == ']')
      {
        var p = a; for (; ; ) { var x = p; x.next(); if (x.n == 0) break; p = x; }
        a.n = (int)(p.s - a.s); a.trim(); return GetType(a, ex).MakeArrayType();
      }
      if (a.s[a.n - 1] == '>')
      {
        var p = a; for (; p.n != 0 && p.s[0] != '<'; p.next()) ; a.n = (int)(p.s - a.s); a.trim(); p.trim(1, 1);
        int n = 0; for (var z = p; z.n != 0; z.next(','), n++) ;
        var u = GetType($"{a}`{n}"); if (u == null && ex) a.error("unknown type");
        var w = new Type[n]; for (int i = 0; p.n != 0; i++) w[i] = GetType(p.next(',')); return u.MakeGenericType(w);
      }
      if (a.equals("void")) return typeof(void);
      if (a.equals("bool")) return typeof(bool);
      if (a.equals("char")) return typeof(char);
      if (a.equals("sbyte")) return typeof(sbyte);
      if (a.equals("byte")) return typeof(byte);
      if (a.equals("int")) return typeof(int);
      if (a.equals("uint")) return typeof(uint);
      if (a.equals("short")) return typeof(short);
      if (a.equals("ushort")) return typeof(ushort);
      if (a.equals("long")) return typeof(long);
      if (a.equals("ulong")) return typeof(ulong);
      if (a.equals("decimal")) return typeof(decimal);
      if (a.equals("float")) return typeof(float);
      if (a.equals("double")) return typeof(double);
      if (a.equals("string")) return typeof(string);
      if (a.equals("object")) return typeof(object);
      var b = a; var s = b.next('.').ToString();
      for (int i = stack.nstats; --i >= 0;)
      {
        var st = ((Type)stack.usings[i]).GetNestedType(s); if (st == null) continue;
        while (b.n != 0) if ((st = st.GetNestedType(b.next('.').ToString())) == null) break; return st;
      }
      var t = GetType(a.ToString().Replace(" ", string.Empty));
      if (t == null && ex) a.error("unknown type");
      return t;
    }
    static Type GetType(string s)
    {
      var ss = stack.usings; var nu = stack.nusings; Type t;
      for (int i = stack.nstats; i < nu; i++) { var u = (string)ss[i]; if (u.StartsWith(s) && (u.Length == s.Length || (u.Length < s.Length && s[u.Length] == '.'))) return null; }
      var a = assemblys ?? (assemblys = AppDomain.CurrentDomain.GetAssemblies());
      for (; ; )
      {
        var x = s.LastIndexOf('.');
        if (x != -1) for (int i = 0; i < a.Length; i++) if ((t = a[i].GetType(s)) != null) return t;
        if (x == -1) for (int i = 0; i < a.Length; i++) for (int k = stack.nstats; k < nu; k++) if ((t = a[i].GetType($"{(string)ss[k]}.{s}")) != null) return t;
        if (x == -1) return null; s = s.Substring(0, x) + '+' + s.Substring(x + 1);
      }
    }
    static MethodBase GetMember(Type type, string name, BindingFlags bf, int xt, int xg)
    {
      var mm = type.GetMember(name ?? ".ctor", name != null ? MemberTypes.Method : MemberTypes.Constructor, bf); if (mm.Length == 0) return null; if (xg == -1) return mm[0] as MethodBase;
      var me = (MethodBase)null; var pp = (ParameterInfo[])null; var vt = (Type)null; var tt = stack.list; int nt = tt.Count - xt, best = int.MaxValue;
      for (int i = 0; i < mm.Length; i++)
      {
        var mt = (MethodBase)mm[i];
        if (stack.usings.Count > xg) if (!mt.IsGenericMethod || mt.GetGenericArguments().Length != stack.usings.Count - xg) continue;
        var pt = mt.GetParameters(); var lt = pt.Length;
        var at = lt != 0 && pt[lt - 1].ParameterType.IsArray && pt[lt - 1].IsDefined(typeof(ParamArrayAttribute), false) ? pt[lt - 1].ParameterType : null;
        if (at != null) if (lt == nt && Convertible(tt[xt + lt - 1].Type, at) != null) at = null; else at = at.GetElementType();
        if (lt < nt && !(at != null && nt >= lt - 1)) continue;
        if (lt > nt && !(pt[nt].HasDefaultValue || (at != null && nt == lt - 1))) continue;
        int t1 = 0, t2 = 0, conv = 0;
        for (int t = 0; t < nt; t++)
        {
          var p1 = tt[xt + t].Type; var p2 = at != null && t >= lt - 1 ? at : pt[t].ParameterType;
          if (p1 == p2) { t1++; continue; }
          if (p2.IsPointer) break;
          if (p2.IsByRef && p2.GetElementType() == p1) { t1++; continue; }
          if (p1 == typeof(Script))
          {
            var a = p2.GetMethod("Invoke")?.GetParameters(); if (a == null) break;
            var b = (Script)((ConstantExpression)tt[xt + t]).Value; b = b.next();
            if (b.s[0] == '(') b.trim(1, 1); int c = 0; for (; b.n != 0; b.next(','), c++) ;
            if (c != a.Length) break; t1++; continue;
          }
          if (p2.ContainsGenericParameters) { t2++; continue; }
          if (p1 == typeof(object) && tt[xt + t] is ConstantExpression && ((ConstantExpression)tt[xt + t]).Value == null) { t1++; continue; } //null
          if (Convertible(p1, p2) != null) { conv += Math.Abs(Type.GetTypeCode(p1) - Type.GetTypeCode(p2)); t2++; continue; }
          break;
        }
        if (t1 + t2 < nt) continue;
        if (conv > best) continue; best = conv;
        if (at != null && me != null) continue;
        me = mt; pp = pt; vt = at; if (t1 == nt && at == null) break;
      }
      if (me == null) return null;
      if (me.IsGenericMethod)
      {
        var aa = me.GetGenericArguments();
        if (stack.usings.Count > xg)
          for (int t = 0; t < aa.Length; t++) aa[t] = (Type)stack.usings[xg + t];
        else
        {
          for (int i = 0, j, f = 0; i < pp.Length && f < aa.Length; i++)
          {
            var t = pp[i].ParameterType; if (!t.ContainsGenericParameters) continue;
            var s = tt[xt + i].Type;
            if (!t.IsConstructedGenericType)
            {
              var ts = s;
              if (t.IsByRef) t = t.GetElementType(); //.HasElementType
              if (t.IsArray && ts.IsArray) { t = t.GetElementType(); ts = ts.GetElementType(); }
              //if (t.HasElementType) t = t.GetElementType();
              for (j = 0; j < aa.Length && !(aa[j].Name == t.Name && aa[j].ContainsGenericParameters); j++) ;
              if (j == aa.Length) continue; aa[j] = ts; f++; continue;
            }
            var a = t.GetGenericArguments();
            if (s == typeof(Script))
            {
              for (j = 0; j < aa.Length && !(aa[j].Name == a[a.Length - 1].Name && aa[j].ContainsGenericParameters); j++) ;
              if (j == aa.Length) continue; var bb = me.GetGenericArguments();
              for (int x = 0, y; x < a.Length; x++) for (y = 0; y < bb.Length; y++) if (bb[y].Name == a[x].Name && a[x].ContainsGenericParameters) { a[x] = aa[y]; break; }
              var r = ((Script)((ConstantExpression)tt[xt + i]).Value).Parse(t.GetGenericTypeDefinition().MakeGenericType(a));
              tt[xt + i] = r; aa[j] = ((LambdaExpression)r).ReturnType; f++; continue;
            }
            //if (!s.IsGenericType) { s = s.GetInterface(t.Name); if (s == null) continue; }
            if (t.IsInterface && t.Name != s.Name) { s = s.GetInterface(t.Name); if (s == null) continue; }
            var v = s.GetGenericArguments(); if (v.Length != a.Length) continue;
            for (int x = 0; x < a.Length; x++) for (int y = 0; y < aa.Length; y++) if (aa[y].Name == a[x].Name && aa[y].ContainsGenericParameters) { aa[y] = v[x]; f++; break; }
          }
        }
        me = ((MethodInfo)me).MakeGenericMethod(aa); pp = me.GetParameters();
      }
      for (int i = 0, n = vt != null ? pp.Length - 1 : pp.Length; i < n; i++)
      {
        var p = pp[i]; var t = p.ParameterType;
        if (i >= nt)
        {
          if (p.DefaultValue == null) { tt.Add(Expression.Default(t.IsByRef ? t.GetElementType() : t)); continue; }
          tt.Add(Expression.Constant(p.DefaultValue, t)); continue;
        }
        var pa = tt[xt + i];
        if (pa.Type == typeof(Script)) { pa = tt[xt + i] = ((Script)((ConstantExpression)pa).Value).Parse(t); continue; }
        tt[xt + i] = Convert(pa, t);
      }
      if (vt != null)
      {
        xt += pp.Length - 1; for (int i = xt; i < tt.Count; i++) tt[i] = Convert(tt[i], vt);
        var e = Expression.NewArrayInit(vt, tt.Skip(xt)); tt.RemoveRange(xt, tt.Count - xt); tt.Add(e);
      }
      if (me.IsSecuritySafeCritical)
      {
        var t = me.DeclaringType;
        if (t == typeof(System.IO.File) ||
            t == typeof(System.IO.Directory))
          throw new Exception("Access denied!");
      }
      return me;
    }
    static Type getvtup(int n) //xxx
    {
      switch (n)
      {
        case 2: return typeof(ValueTuple<,>);
        case 3: return typeof(ValueTuple<,,>);
        case 4: return typeof(ValueTuple<,,,>);
        case 5: return typeof(ValueTuple<,,,,>);
        case 6: return typeof(ValueTuple<,,,,,>);
        case 7: return typeof(ValueTuple<,,,,,,>);
        case 8: return typeof(ValueTuple<,,,,,,,>);
      }
      return null;
    }
    static void addtup(int c, Script v) { if (stack.tuplnames == null) for (int i = 0; i < c; i++) stack.tuplnames += ','; stack.tuplnames += ','; stack.tuplnames += v.ToString(); }
    internal static Type[] GetExtensions()
    {
      if (extensions == null)
        extensions = new Type[] { typeof(CSG), typeof(CDX), typeof(Enumerable) };//, typeof(ParallelEnumerable) };
      //extensions = assemblys.SelectMany(p => p.GetExportedTypes()).Where(p => p.GetCustomAttribute(typeof(System.Runtime.CompilerServices.ExtensionAttribute)) != null).ToArray();
      return extensions;
    }
    static Assembly[] assemblys; static Type[] extensions;
    static object Convertible(Type ta, Type tb, bool imp = true)
    {
      if (tb.IsAssignableFrom(ta)) return tb;
      int b = (int)Type.GetTypeCode(tb) - 4;
      if (b < 0)
      {
        if (tb.IsByRef) return Convertible(ta, tb.GetElementType(), imp);
        if (!imp) return null;
        if (!ta.IsValueType || !tb.IsValueType) return null; //Debug.WriteLine(ta + " " + tb);
        for (int j = 0; j < 2; j++)
        {
          var aa = (j == 0 ? tb : ta).GetMember("op_Implicit", MemberTypes.Method, BindingFlags.Static | BindingFlags.Public);
          for (int i = 0; i < aa.Length; i++)
            if (Convertible(ta, ((MethodBase)aa[i]).GetParameters()[0].ParameterType, false) != null) return aa[i];
        }
        return null;
      }
      int a = (int)Type.GetTypeCode(ta) - 4, m;
      switch (a)
      {
        case 0: m = 0b111111110000; break;
        case 1: m = 0b111010101000; break;
        case 2: m = 0b111111111001; break;
        case 3: m = 0b111010100000; break;
        case 4: m = 0b111111100000; break;
        case 5: m = 0b111010000000; break;
        case 6: m = 0b111110000000; break;
        case 7: m = 0b111000000000; break;
        case 8: m = 0b111000000000; break;
        case 9: m = 0b010000000000; break;
        default: return null;
      }
      return (m & (1 << b)) != 0 ? tb : null;
    }
    static Expression Convert(Expression a, Type t)
    {
      if (a.Type == t) return a;
      if (t == typeof(object) && !a.Type.IsClass) return Expression.Convert(a, t);
      if (t.IsAssignableFrom(a.Type)) return a;
      if (t.IsByRef)
      {
        t = t.GetElementType();
        return Convert(a, t);
      }
      if (a is ConstantExpression c)
      {
        var v = c.Value; if (v == null) return Expression.Constant(v, t);
        if (t.IsPrimitive && v is IConvertible) return Expression.Constant(System.Convert.ChangeType(v, t), t);
      }
      var x = Convertible(a.Type, t); if (x == null) throw new Exception("invalid conversion");
      if (x is MethodInfo me) return Expression.Convert(Convert(a, me.GetParameters()[0].ParameterType), t, me);
      return Expression.Convert(a, t);
    }
    void check(int stackab)
    {
      if (!isname()) error();
      for (int i = stackab; i < stack.Count; i++) if (stackequals(stack[i].Name)) error("duplicate name");
    }
    class Exception : System.Exception { public Exception(string s) : base(s) { } }
    void error(string s = null) { if (n <= 0) n = 1; *(Script*)ptr = this; throw new Exception(s ?? "syntax"); }
    Script next() { var a = this; a.n = token(); s += a.n; n -= a.n; trim(); return a; }
    Script next(char c) { var p = this; for (; n != 0;) { var t = next(); if (t.n == 1 && t.s[0] == c) { p.n = (int)(t.s - p.s); p.trim(); break; } } return p; }
    Script block()
    {
      var p = this;
      for (var e = true; n != 0;)
      {
        //if (s[0] == ')' || s[0] == '}') { n = 1; error(); }
        var t = next(); if (t.s[0] == ';') { p.n = (int)(t.s - p.s); p.trim(); break; }
        if (e && t.s[t.n - 1] == '}') { p.n = (int)(t.s + t.n - p.s); p.trim(); break; }
        if (t.n == 3 && t.equals("new")) e = false;
      }
      return p;
    }
    Script gettype()
    {
      for (var t = this; ;)
      {
        var a = next(); if (n != 1 && s[0] == '.') { next(); continue; }
        if (a.isname() && (isname() || (n != 1 && (s[0] == '<' || s[0] == '['))))
        {
          if (map != null && a.s != t.s) { for (int i = 0; i < keywords.Length; i++) if (a.equals(keywords[i])) goto r; }
          if (s[0] == '<') next();
          while (n != 0 && (s[0] == '[')) { var i = next(); i.trim(1, 1); if (i.n != 0) goto r; }
          t.n = (int)(s - t.s); t.trim(); return t;
        }
        if (a.s[0] == '(') { var b = a; b.trim(1, 1); b.next(','); if (b.n != 0) return a; } //xxx (int a, int b)
      r: this = t; t.n = 0; return t;
      }
    }
    bool equals(string name)
    {
      var l = name.Length; if (l != n) return false;
      for (int i = 0; i < l; i++) if (s[i] != name[i]) return false; return true;
    }
    bool stackequals(string name) //xxx
    {
      var l = name.IndexOf(','); if (l == -1) l = name.Length; else { }
      if (l != n) return false;
      for (int i = 0; i < l; i++) if (s[i] != name[i]) return false; return true;
    }
    int token()
    {
      if (n == 0) return 0;
      int x = 0; var c = s[x++];
      //if(false)
      //if (c == '<')
      //  for (int i = 1, k = 1; i < n; i++) 
      //    if (s[i] == ';') break;
      //    else if (s[i] == '<') k++; 
      //    else if (s[i] == '>' && --k == 0) return i + 1;
      if (c == '(' || c == '{' || c == '[')
      {
        for (int k = 1; x < n;)
        {
          c = s[x++];
          if (c == '"' || c == '\'') { for (char v; x < n && (v = s[x++]) != c;) if (v == '\\') x++; continue; }
          if (c == '(' || c == '{' || c == '[') { k++; continue; }
          if (c == ')' || c == '}' || c == ']') { if (--k == 0) break; continue; }
        }
        return x;
      }
      if (c == '"' || c == '\'') { for (char v; x < n && (v = s[x++]) != c;) if (v == '\\') x++; return x; }
      if (char.IsLetter(c) || c == '_') { for (; x < n && (char.IsLetterOrDigit(c = s[x]) || c == '_'); x++) ; return x; }
      if (c == '0' && x < n && (s[x] | 0x20) == 'x') { for (++x; x < n && char.IsLetterOrDigit(c = s[x]); x++) ; return x; }
      if (char.IsNumber(c) || (c == '.' && x < n && char.IsNumber(s[x])))
      {
        for (; x < n && char.IsNumber(c = s[x]); x++) ;
        if (c == '.') for (++x; x < n && char.IsNumber(c = s[x]); x++) ;
        if ((c | 0x20) == 'e') { for (++x; x < n && char.IsNumber(c = s[x]); x++) ; if (c == '+' || c == '-') x++; for (; x < n && char.IsNumber(c = s[x]); x++) ; }
        c |= (char)0x20; if (c == 'f' || c == 'd' || c == 'm') x++;
        return x;
      }
      if (c == ',' || c == ';' || c == '.') return x;
      if (c == '<')
      {
        for (int k = 1, y = x; y < n;)
        {
          var z = s[y++]; if (z == '<') { if (s[y - 2] == z) break; k++; continue; }
          if (z == '>') { if (--k == 0) return y; continue; }
          if (!(z == '_' || char.IsLetterOrDigit(z) || z <= ' ' || z == '.' || z == ',' || z == '[' || z == ']')) break;
        }
      }
      if (x == n) return x;
      if (c == '+' || c == '-' || c == '=') { if (s[x] == c) return x + 1; if (c != '=' && s[x] != '=') return x; }
      for (; x < n && (c = s[x]) > ' ' && c != '(' && c != '.' && c != '_' && c != '"' && c != '\'' && !char.IsLetterOrDigit(c); x++) ;//for (; x < e && "/=+-*%&|^!?:<>~".IndexOf(code[x]) >= 0; x++) ;
      return x;
    }
    int opcode()
    {
      switch (n)
      {
        case 1: switch (s[0]) { case '+': return 0x30; case '-': return 0x31; case '*': return 0x20; case '/': return 0x21; case '%': return 0x22; case '=': return 0xd0; case '?': return 0xc0; case '|': return 0x90; case '^': return 0x80; case '&': return 0x70; case '<': return 0x50; case '>': return 0x51; } break;
        case 2:
          if (s[1] == '=') { switch (s[0]) { case '*': return 0xd1; case '/': return 0xd2; case '%': return 0xd3; case '+': return 0xd4; case '-': return 0xd5; case '&': return 0xd6; case '^': return 0xd7; case '|': return 0xd8; case '=': return 0x60; case '!': return 0x61; case '<': return 0x52; case '>': return 0x53; } break; }
          if (s[1] == 's') { switch (s[0]) { case 'i': return 0x54; case 'a': return 0x55; } break; }
          if (s[0] == s[1]) { switch (s[0]) { case '<': return 0x40; case '>': return 0x41; case '&': return 0xb0; case '?': return 0xc2; case '|': return 0xa0; } break; }
          if (s[0] == '=' && s[1] == '>') return 0xdf;
          break;
        case 3: if (s[2] == '=' && s[0] == s[1]) switch (s[0]) { case '<': return 0xd9; case '>': return 0xda; } break;
      }
      return 0;
    }
    void trim()
    {
      for (; n != 0 && *s <= 32; s++, n--) ;
      for (; n != 0 && s[n - 1] <= 32; n--) ;
    }
    void trim(int a, int b = 0)
    {
      s += a; n -= a + b; if (n < 0) error(); trim();
    }
    bool isname()
    {
      return n != 0 && (char.IsLetter(s[0]) || s[0] == '_');
    }
    bool take(string v) { var l = v.Length; if (l > n) return false; for (int i = 0; i < l; i++) if (s[i] != v[i]) return false; s += l; n -= l; trim(); return true; }
    internal static readonly string[] keywords = {
      "void","bool","char","sbyte","byte","int","uint","short","ushort","long","ulong","decimal","float","double","string","object","dynamic", //17
      "var","this","base","public","protected","private","internal","readonly", "static",
      "if", "for","while","foreach","switch","return","using","checked","unchecked",
      "true","false","null","as","is","in","new","typeof","try","throw","catch","else","case","break","default",
      "continue","finally","get","set","value","ref","out","sizeof","stackalloc","class","struct","lock","goto","fixed","const"};
    #region debug extension
    int index => (int)(s - (char*)(ptr + 32));
    internal static List<(int i, int n, int v, object p)> map; internal static int[] bps;
    internal static Func<int, (int id, object p)[], bool> dbg;
    static void __map(Script s, int v, object p)
    {
      var i = s.index; for (int t = 0; t < map.Count; t++)
      {
        var m = map[t]; if (m.i != i) continue; if (m.v == v) return;
        if (m.v == 0x08) { map.RemoveAt(t); break; } //!ns
      }
      map.Add((i, s.n, v, p));
    }
    static void __map(Script s, ParameterExpression p, int id)
    {
      if (!stack.dict.TryGetValue(p, out var i)) stack.dict[p] = i = stack.dict.Count + 1;
      if (id == 0) for (int t = 0; t < map.Count; t++) if (map[t].v >> 8 == i) { id = map[t].v; break; }
      if (id == 0) id = 0x06; //this
      __map(s, id | (i << 8), p.Name.Contains(',') ? (object)new Tuple<Type, string>(p.Type, p.Name) : p.Type); //xxx
    }
    static void __map(Script p)
    {
      if (map != null) { var t = __dbg(p); if (t != null) stack.list.Add(t); }
    }
    static Expression __dbg(Script s)
    {
      if (map == null) return null; var e = __dbg(map.Count);
      __map(s, bps != null && Array.IndexOf(bps, s.index) != -1 ? 0x1A : 0x0A, null); return e;
    }
    static Expression __dbg(int i)
    {
      if (dbg == null) return null;
      var t1 = Expression.Constant(dbg.Target); var t2 = Expression.Constant(i);
      return Expression.IfThen(
        Expression.Call(t1, dbg.Method, t2, Expression.Default(typeof((int, object)[]))),
        Expression.Call(t1, dbg.Method, t2,
          Expression.NewArrayInit(typeof((int, object)),
            stack.Skip(stack.xpos).Intersect(stack.dict.Keys).//Where(p => !p.Type.IsSubclassOf(typeof(Delegate))).
            Select(p => Expression.New(__ctor ?? (__ctor = typeof((int, object)).GetConstructors()[0]),
              Expression.Constant(stack.dict[p]), Expression.Convert(p, typeof(object)))))
          ));
    }
    static void __dbg()
    {
      var t1 = Expression.NewArrayInit(typeof((int, object)),
                stack.Intersect(stack.dict.Keys).Where(p => !p.Type.IsSubclassOf(typeof(Delegate))).
                Select(p => Expression.New(__ctor ?? (__ctor = typeof((int, object)).GetConstructors()[0]),
                  Expression.Constant(stack.dict[p]), Expression.Convert(p, typeof(object)))));
      var t2 = Expression.Lambda<Func<(int, object)[]>>(t1, "?", null);
      stack.list.Insert(0, Expression.Assign(stack[stack.xpos], t2));
    }
    static ConstructorInfo __ctor;
    #endregion
  }

  unsafe class ScriptEditor : CodeEditor
  {
    internal Node node; int hccode;
    protected override void UpdateSyntaxColors()
    {
      if (hccode == 0) hccode = EditText.GetHashCode();
      if (colormap == null) colormap = new int[] { 0, 0x00007000, 0x00000088, 0x00ff0000, 0x11463a96, 0x2200ddff, 0x00af912b, 0x000000ff };
      int n = text.Length; Array.Resize(ref charcolor, n + 1); if (n == 0) map.Clear();
      for (int i = 0; i < n; i++)
      {
        var c = text[i];
        if (c <= ' ') { charcolor[i] = 0; continue; }
        if (c == '/' && i + 1 < n)
        {
          if (text[i + 1] == '/') { for (; i < n && text[i] != 10; i++) charcolor[i] = 1; continue; }
          if (text[i + 1] == '*') { var t = text.IndexOf("*/", i + 2); t = t >= 0 ? t + 2 : n; for (; i < t; i++) charcolor[i] = 1; i = t - 1; continue; }
        }
        if (c == '"' || c == '\'')
        {
          var x = i; for (++i; i < n; i++) { var t = text[i]; if (t == '\\') { i++; continue; } if (t == c) break; }
          for (; x <= i; x++) charcolor[x] = 2; continue;
        }
        var l = i; for (; l < n && IsLetter(text[l]); l++) ;
        var r = l - i; if (r == 0) { if (error == null || charcolor[i] != 6) charcolor[i] = 0; continue; }
        byte color = 0;
        if (r > 1 && !(l < text.Length && char.IsDigit(text[l])))
          for (int t = 0; t < Script.keywords.Length; t++)
          {
            var kw = Script.keywords[t];
            if (kw.Length == r && kw[0] == text[i] && string.Compare(text, i, kw, 0, kw.Length, true) == 0) { color = 3; break; }
          }

        for (; i < l; i++) if (error == null || charcolor[i] != 6) charcolor[i] = color; i--;
      }
      for (int i = 0; i < map.Count; i++)
      {
        ref var m = ref pmap[i]; if (m.v == 0 && m.p is Type && charcolor[m.i] != 3) color(m.i, m.n, 6);
        if (overid != 0 && (overid & 0x0f) < 8 && overid == (m.v & ~0xf0)) color2(m.i, m.n, 0x80);
      }
      for (int i = 0; i < map.Count; i++) { ref var m = ref pmap[i]; if ((m.v & 0xf) == 0x0A && (m.v & 0x30) != 0) color(m.i, m.n, (byte)((m.v & 0x20) != 0 ? 5 : 4)); }
      if (error != null) color2(epos.i, epos.n, 0x70);// for (int i = 0; i < epos.n && epos.i + i < charcolor.Length; i++) charcolor[epos.i + i] |= 0x70;
    }
    protected override int GetRange(int x)
    {
      for (int i = x, n = text.Length; i < n; i++)
      {
        if (text[i] == 10 || text[i] == ';') break;
        if (text[i] == '/' && i + 1 < n)
        {
          if (text[i + 1] == '*') { for (i += 2; i < n - 1; i++) if (text[i] == '*' && text[i + 1] == '/') break; return -i; }
          if (text[i + 1] == '/') { for (int t; (t = nextls(i)) < n - 1 && text[t] == '/' && text[t + 1] == '/'; i = t) ; return -i; }
        }
        if (text[i] == '[') { for (int t; (t = nextls(i)) < n && text[t] == '['; i = t) ; return -i; }
        if (IsLetter(text[i]))
        {
          if (startsw(i, "using")) { for (int t; (t = nextls(i)) < n && startsw(t, "using"); i = t) ; return -i; }
          for (int k2 = 0; i < n; i++)
          {
            var c = text[i];
            if (c == '(') { k2++; continue; }
            if (c == ')') { k2--; continue; }
            if (c == ';' && k2 == 0) break;
            if (c == '{')
            {
              for (int k = 0; i < n; i++)
              {
                if (text[i] == '{') { k++; continue; }
                if (text[i] == '}') { if (--k == 0) return i; }
              }
              break;
            }
          }
          break;
        }
        //if (text[i] == '<') //inline xml
        //{
        //  if (i + 1 == n || text[i + 1] == '/') break;
        //  for (int z = 0, k; i < n; i++)
        //  {
        //    if (text[i] == '<')
        //    {
        //      for (k = i + 1; k < n && text[k] != '>'; k++) ; if (k == n) break;
        //      if (text[i + 1] == '!') { i = k; continue; }
        //      if (text[i + 1] == '/') z--; else if (text[k - 1] != '/') z++;
        //      i = k; continue;
        //    }
        //    if (z == 0) return i;
        //  }
        //}
      }
      return 0;
    }
    int nextls(int i)
    {
      for (; i < text.Length && text[i] != 10; i++) ;
      for (; i < text.Length && text[i] <= 32; i++) ;
      return i;
    }
    bool startsw(int i, string s)
    {
      return i + s.Length <= text.Length && string.CompareOrdinal(s, 0, text, i, s.Length) == 0;
    }
    int rebuild = 5; (int i, int n) epos; string error;
    protected override void Replace(int i, int n, string s)
    {
      var nt = text.Length - n + s.Length;
      var nc = charcolor.Length; if (nc < nt + 1) Array.Resize(ref charcolor, nt + 1);
      Array.Copy(charcolor, i + n, charcolor, i + s.Length, nc - (i + n));
      for (int t = 0; t < map.Count; t++)
      {
        ref var m = ref pmap[t]; if (m.i + m.n <= i) continue;
        if (m.i >= i + n) { m.i += s.Length - n; continue; }
        m.n += s.Length - n; if (m.n <= 0) map.RemoveAt(t--);
      }
      EndToolTip(); base.Replace(i, n, s);
      rebuild = s.Length == 1 && (s[0] == '.' || s[0] == '(') ? 2 : 5;
    }
    protected override void WndProc(ref Message m)
    {
      base.WndProc(ref m);
      if (m.Msg == 0x0113)//WM_TIMER
      {
        if (rebuild != 0 && --rebuild == 0) { build(); postbuild(); }
        ontimer?.Invoke();
      }
    }
    protected override unsafe void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e); int k = -1;
      for (int i = 0; i < map.Count; i++)
      {
        ref var p = ref pmap[i]; if ((p.v & 0x0F) != 0x0A) continue;
        if ((p.v & 0x20) != 0) k = i; if ((p.v & 0x10) == 0) continue;
        var y = LineOffset(LineFromPos(p.i)); TypeHelper.drawicon(e.Graphics, -1, y, 11, dpiscale(20));
      }
      if (k != -1) TypeHelper.drawicon(e.Graphics, -2, LineOffset(LineFromPos(pmap[k].i)) + 1, 12, dpiscale(20));
    }
    public override int OnCommand(int id, object test)
    {
      switch (id)
      {
        //case 1024: //SaveProjectItem
        //  if (test != null) return 1;
        //  return 1;
        case 5011: // Run Without Debugging Strg+F5
        case 5010: // Run Debugging F5
          if (id == 5011 && state == 7) return 0;
          if (state != 7)
          {
            if (error == null && hccode == EditText.GetHashCode()) return 0;
            if (test != null) return 1;
            if (error != null)
            {
              Select(epos.i, epos.i + epos.n); ScrollVisible();
              node.view.MessageBox(error);
              return 1;
            }
            OnLostFocus(null); return 1;
          }
          if (test != null) return 1;
          EndFlyer(); ontimer = null; state = 0; sp = null; return 1;
        case 5016: //Step Over F10
          if (state != 7) return 0;
          if (test != null) return 1;
          EndFlyer(); ontimer = null; state = 1; return 1;
        case 5015: //Step In F11
          if (state != 7) return 0;
          if (test != null) return 1;
          EndFlyer(); ontimer = null; state = 2; return 1;
        case 5017: // Step Out Shift F11 
          if (state != 7) return 0;
          if (test != null) return 1;
          EndFlyer(); ontimer = null; state = 3; return 1;
        case 5013: // Stop Debugging
          if (state != 7) return 0;
          if (test != null) return 1;
          state = 0; clearbreakpoints(test);
          //EndFlyer(); ontimer = null; state = 0; sp = null;
          //for (int i = 0; i < map.Count; i++) pmap[i].v &= ~0x10;
          //UpdateSyntaxColors(); Invalidate();
          return 1;
        case 5020: return breakpoint(test);
        case 5021: return clearbreakpoints(test);
        case 5022: return ongotodef(test);
        case 5023: return onrename(test);
        case 2000: return onhelp(test);
          //case 5025: return onshowil(test);
      }
      return base.OnCommand(id, test);
    }
    int breakpoint(object test)
    {
      if (error != null) return 0;
      int x = SelectionStart, l = LineFromPos(x), a = GetLineStart(l);
      var spots = map.Where(p => (p.v & 0x0f) == 0x0A);
      var sp = spots.Where(p => p.i <= x && p.i + p.n >= x && (p.v & 0x10) != 0).OrderBy(p => p.n).LastOrDefault();
      if (sp.n == 0) sp = spots.Where(p => (x == a ? p.i < x : p.i <= x) && p.i + p.n >= x).OrderBy(p => p.n).FirstOrDefault();
      if (sp.n == 0) sp = spots.Where(p => LineFromPos(p.i) == l && (p.v & 0x10) != 0).OrderBy(p => p.i).FirstOrDefault();
      if (sp.n == 0) sp = spots.Where(p => LineFromPos(p.i) == l).OrderBy(p => p.i).FirstOrDefault();
      if (sp.n == 0) return 0; if (test != null) return 1;
      var i = map.IndexOf(sp); sp.v ^= 0x10; map[i] = sp; UpdateSyntaxColors(); Invalidate();
      if ((sp.v & 0x10) != 0) OnLostFocus(null);
      return 1;
    }
    int clearbreakpoints(object test)
    {
      if (map == null || !map.Any(p => (p.v & 0x10) != 0)) return 0;
      if (test != null) return 1;
      for (int i = 0; i < map.Count; i++) pmap[i].v &= ~0x10;
      UpdateSyntaxColors(); Invalidate(); return 1;
    }
    void build()
    {
      Script.bps = map.Where(p => p.v == 0x1A).Select(p => p.i).ToArray();
      (Script.map = map).Clear(); error = null;
      try { var e = Script.Compile(node.GetType(), EditText); }
      catch (Exception e) { epos = Script.LastError; error = e.Message; }
      pmap = ((int i, int n, int v, object p)[])map.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
      checkover(); UpdateSyntaxColors(); Invalidate();
    }
    internal int getlineflags() { int f = 0; for (int i = 0; i < lineflags.Length; i++) f |= lineflags[i]; return f; }
    List<(int i, int n, int v, object p)> map = new List<(int i, int n, int v, object p)>(); (int i, int n, int v, object p)[] pmap;
    int state; int* sp; (int id, object p)[] stack; Action ontimer; int lastpos = -1, overid;[ThreadStatic] static readonly bool MainThread = true;
    bool DebugStep(int i, (int id, object p)[] stack)
    {
      if (!visible) return false;// i >= map.Count) return false;
      ref var m = ref pmap[i];
      if (stack == null)
      {
        if ((m.v & 0x10) == 0)
        {
          if (state == 0) return false;
          if (state == 1 && sp > &i) return false; // + 1
          if (state == 3 && sp >= &i) return false;
        }
        if (state == 7) return false;
        if (!MainThread) return false;
        return true;
      }
      ReadOnly = true; //var t1 = node.funcs; node.funcs = Array.Empty<object>();
      m.v |= 0x20; Select(m.i); UpdateSyntaxColors(); Invalidate(); ScrollVisible();
      if (pane.Frame is IVsWindowFrame f) f.Show();
      node.view.Visible = false; ApexPackage.Package.CanClose += CanClose;
      this.stack = stack; sp = &i;
      for (state = 7; state == 7;) { Native.WaitMessage(); Application.DoEvents(); }
      this.stack = null; m.v &= ~0x20; ReadOnly = false; UpdateSyntaxColors(); Invalidate(); Update();
      node.view.Visible = true; ApexPackage.Package.CanClose -= CanClose; //node.funcs = t1;
      return true;
    }
    void CanClose(bool[] ok)
    {
      if (state != 7) return;
      ok[0] = false; //node.view.MessageBox("Stop debugging!"); 
      if (MessageBox.Show(this, "Stop Debugging?", "Script", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        OnCommand(5013, null);
    }
    bool visible; internal void show(bool on) { visible = on; if (!visible) state = 0; }
    void color(int i, int n, byte c) { for (int t = 0; t < n; t++) { charcolor[i + t] = c; } }
    void color2(int i, int n, int c) { for (int b = Math.Min(i + n, charcolor.Length); i < b; i++) charcolor[i] |= (byte)c; }
    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e); //if (flyer != null) return;
      var x = PosFromPoint(e.Location);
      if (error != null && x >= epos.i && x <= epos.i + epos.n)
      {
        if (lastpos == epos.i) return; lastpos = epos.i;
        var s = error; SetToolTip(TextBox(epos.i, epos.n), t => t == 0 ? $"{"Error"} {s}" : null);
        return;
      }
      for (int i = 0; i < map.Count; i++)
      {
        var m = pmap[i]; if ((m.v & 0xf) == 0x0A || m.i > x || m.i + m.n < x) continue;
        if (lastpos == m.i) return; lastpos = m.i;
        if (state == 7 && (m.v >> 8) != 0)
        {
          var g = ((Func<(int id, object p)[]>)(stack[0].p))();
          var f = m.v >> 8; var v = g.Concat(stack).FirstOrDefault(p => p.id == f);
          if (v.id != 0)
          {
            EndToolTip(); EndFlyer(); //ToolTip(text.Substring(tpos.i, tpos.n) + " = " + p.ToString());
            var ri = RectangleToScreen(TextBox(m.i, m.n)); int ticks = 0;
            ontimer = () =>
            {
              if (ticks++ < 5) return;
              ontimer = null; EndToolTip(); EndFlyer();
              flyer = new TypeExplorer { Location = new Point(ri.X, ri.Bottom) }; var mi = m.p as MemberInfo;
              flyer.items = new TypeExplorer.Item[] { new TypeExplorer.Item { icon = mi != null ? TypeHelper.image(mi) : 8 /*18*/, text = text.Substring(m.i, m.n), obj = v.p, info = m.p } };
              flyer.Show(); flyer.ontooltip = (r, item) => SetToolTip(RectangleToClient(r), xx => xx == 0 ? item.pv as string ?? item.sv : null);
            };
            return;
          }
        }
        EndFlyer(); ontimer = null;
        SetToolTip(TextBox(m.i, m.n), t =>
        {
          if (t != 0) return null;
          var s = TypeHelper.tooltip(m, text);
          //if (epos.n != 0) s = string.Format("{0}{1}:\n  {2}", s != null ? string.Format("{0}\n\n", s.Trim()) : null, (epos.v & 4) != 0 ? "Error" : "Warning", _error(epos));
          return s;// + $" ({m.v >> 8})";
        });
        return;
      }
      lastpos = -1;
    }
    void postbuild()
    {
      if (SelectionLength != 0) return;
      var s = text; var i1 = SelectionStart; var i2 = i1; var cv = i1 != 0 ? s[i1 - 1] : '\0';

      if (cv == '(')
      {
        var px = map.Where(p => p.i + p.n <= i1 - 1).OrderBy(p => p.i + p.n).LastOrDefault();
        if (px.n != 0 && px.v == 0 && text.Substring(px.i + px.n, i1 - 1 - (px.i + px.n)).Trim().Length == 0)
        {
          if (px.p is Type)
          {
            var cc = ((Type)px.p).GetConstructors(); if (cc.Length == 0) return; int l = 0;
            SetToolTip(Caret, t =>
            {
              if (t == 0) return TypeHelper.tooltip(cc[l]);
              if (t == 1) return string.Format("{0} of {1}", l + 1, cc.Length);
              if (t == 40) { if (++l == cc.Length) l = 0; }
              if (t == 38) { if (l-- == 0) l = cc.Length - 1; }
              return null;
            }, 2);
          }
          if (px.p is MethodInfo mi)
          {
            var type = mi.DeclaringType;
            var items = type.GetMethods((!mi.IsStatic ? BindingFlags.Instance : BindingFlags.Static) | BindingFlags.Public).Where(p => p.Name == mi.Name);
            if (px.v != 0) items = items.Concat(Extensions(type, mi.Name));
            var mm = items.ToArray(); if (mm.Length == 0) return; int x = Array.IndexOf(mm, mi); if (x < 0) x = 0;
            SetToolTip(Caret, t =>
            {
              if (t == 0) return TypeHelper.tooltip(mm[x]);
              if (t == 1) return string.Format("{0} of {1}", x + 1, mm.Length);
              if (t == 40) { if (++x == mm.Length) x = 0; }
              if (t == 38) { if (x-- == 0) x = mm.Length - 1; }
              return null;
            }, 2);
            return;
          }
        }
        return;
      }
      if (flyer != null) return;
      if (cv == '.')
      {
        var tp = map.FirstOrDefault(p => p.i + p.n + 1 == i1); if (tp.n == 0) return;
        if (tp.p is string ns)
        {
          var usings = map.Where(p => p.v == 0x88).Select(p => (string)p.p);
          var aa = new Assembly[] { typeof(CDX).Assembly, typeof(int).Assembly, typeof(Control).Assembly, typeof(Point).Assembly, typeof(Trace).Assembly };
          var tt = aa.SelectMany(a => a.GetTypes()).Where(t => t.IsPublic && !t.IsNested && t.Namespace != null);
          var ii = tt.Select(p => p.Namespace).
            Where(p => p.Length > ns.Length && p[ns.Length] == '.' && p.StartsWith(ns)).
            Where(p => (tp.v & 0x0f) == 0x08 || usings.Contains(p)).
            Select(p => p.Substring(ns.Length + 1, (int)Math.Min((uint)p.IndexOf('.', ns.Length + 1), (uint)p.Length) - (ns.Length + 1))).
            Distinct().Select(p => new TypeExplorer.Item { icon = 3, text = p, info = string.Format("{0}.{1}", ns, p) });
          if (tp.v == 0x08) ii = ii.Concat(tt.
             Where(p => p.Namespace == ns).
             GroupBy(p => p.Name.Contains('`') ? p.Name.Substring(0, p.Name.IndexOf('`') + 1) : p.Name).
             Select(p => p.ToArray()).
             Select(p => new TypeExplorer.Item { icon = TypeHelper.image(p[0]), text = TypeHelper.shortname(p[0], false), info = p }));
          EditFlyer(i1, i2, ii.OrderBy(p => p.text).ToArray());
          return;
        }
        var type = tp.p as Type ?? (tp.p is PropertyInfo pi ? pi.PropertyType : tp.p is FieldInfo fi ? fi.FieldType : tp.p is Tuple<Type, string> xx ? xx.Item1 : null);
        if (type == null) return;
        var items = type.GetMembers((tp.v != 0 ? BindingFlags.Instance : BindingFlags.Static | BindingFlags.FlattenHierarchy) |
          (type == node.GetType() ? BindingFlags.Public | BindingFlags.NonPublic : BindingFlags.Public)).
          Where(p => TypeHelper.__filter(p, cv == '#')).GroupBy(p => p.Name).Select(p => p.ToArray()).
          Select(p => new TypeExplorer.Item { icon = TypeHelper.image(p[0]), text = p[0].Name, info = p });
        if (tp.v != 0) items = items.Concat(Extensions(type).GroupBy(p => p.Name).Select(p => p.ToArray()).
          Select(p => new TypeExplorer.Item { icon = 24, text = p[0].IsGenericMethod ? p[0].Name + "<>" : p[0].Name, info = p }));
        if (tp.p is Tuple<Type, string> ts)//xxx
          items = items.Concat(ts.Item2.Split(',').Skip(1).Zip(ts.Item1.GetFields(), (n, f) => new TypeExplorer.Item { icon = 8, text = n, info = f }).Where(p => p.text.Length != 0));
        EditFlyer(i1, i2, items.OrderBy(p => p.text).ToArray()); return;
      }
    }
    IEnumerable<MethodInfo> Extensions(Type type, string name = null)
    {
      return Script.GetExtensions().SelectMany(t => t.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod)).OfType<MethodInfo>().
        Where(m =>
        {
          if (name != null && m.Name != name) return false;
          if (!m.IsDefined(typeof(ExtensionAttribute), false)) return false;
          var t = m.GetParameters()[0].ParameterType;
          if (!m.IsGenericMethod) { if (t.IsAssignableFrom(type)) return true; return false; }
          if (t.IsAssignableFrom(type)) return true;
          if (type.GetInterface(t.Name) != null) return true;
          if (t.IsGenericType && type.IsGenericType && type.GetGenericTypeDefinition() == t.GetGenericTypeDefinition()) return true;
          return false;
        });
    }
    TypeExplorer flyer;
    internal ScriptToolWindowPane pane;
    protected internal override void OnSelChanged()
    {
      base.OnSelChanged(); if (rebuild == 0 && checkover()) { UpdateSyntaxColors(); Invalidate(); }
    }
    bool checkover()
    {
      int i1 = SelectionStart, i2 = i1 + SelectionLength;
      var t = map.Count; while (--t >= 0) { ref var p = ref pmap[t]; var f = p.v & 0x0f; if (f != 1 && f != 0xa && i1 >= p.i && i2 <= p.i + p.n) break; }
      int id = t >= 0 ? pmap[t].v : 0; id = id & ~0xf0;
      if (overid != id) { overid = id; return true; }
      return false;
    }
    int ongotodef(object test)
    {
      if (overid == 0 || (overid & 0xf) == 8) return 0;
      if ((overid & 0x80000000) != 0) return 0;
      if (test != null) return 1;
      var t = map.FirstOrDefault(p => (p.v & ~0xf0) == overid && (p.v & 0x80) != 0);
      if (t.n == 0) t = map.FirstOrDefault(p => (p.v & ~0xf0) == overid);
      Select(t.i, t.i + t.n); ScrollVisible();
      return 1;
    }
    int onrename(object test)
    {
      if (ReadOnly || error != null || overid == 0 || (overid & 0xf) == 8) return 0;
      if (test != null) return 1;
      var a = map.Where(p => (p.v & ~0xf0) == overid);
      var f = a.FirstOrDefault(); if (f.n == 0) return 1;
      var s = text.Substring(f.i, f.n); var b = a.Select(t => new Point(t.i, t.n));
      TextEnter("Rename", s, v => Replace(b, v)); return 1;
    }
    int onhelp(object test)
    {
      if (error != null) return 0;
      //var ep = errors.FirstOrDefault(p => SelMin >= p.i && SelMax <= p.i + p.n);
      //if ((ep.v >> 8) != 0) { if (test != null) return 1; TypeHelper.msdn("CS" + (ep.v >> 8).ToString("0000")); return 1; }
      var tpos = map.FirstOrDefault(p => p.p != null && SelMin >= p.i && SelMax <= p.i + p.n);
      var m = tpos.p as MemberInfo;
      if (m == null)
      {
        var ps = WordFromPoint(SelMin); if (ps.X == ps.Y || SelMax > ps.Y) return 0;
        var ss = text.Substring(ps.X, ps.Y - ps.X); if (!Script.keywords.Contains(ss)) return 0;
        if (test != null) return 1; TypeHelper.msdn(ss + "_CSHARPKEYWORD"); return 1;
      }
      var t = m as Type ?? m.DeclaringType; if (t == null) return 1; //dm
      //if (t.DeclaringType == typeof(Compiler)) { if (test == null) TypeHelper.msdn(t.Name + "_CSHARPKEYWORD"); return 1; }
      if (!t.Assembly.GlobalAssemblyCache) return 0; //todo: navigate to own documentation
      if (test != null) return 1; TypeHelper.msdn(string.Format(m != t ? "{0}.{1}.{2}" : "{0}.{1}", t.Namespace, t.Name, m.Name)); return 1;
    }
    void EditFlyer(int i1, int i2, TypeExplorer.Item[] items)
    {
      if (items.Length == 0) return; var t2 = i2; ontimer = null;
      EndFlyer(); EndToolTip();
      flyer = new TypeExplorer(); var ri = RectangleToScreen(Caret);
      flyer.Location = new Point(ri.X, ri.Bottom);
      flyer.items = items; flyer.noeval = true;
      flyer.Show();
      flyer.onclick = e => { EndFlyer(); Select(i1, i2); Paste(e.text.Split('<')[0]); return true; };
      flyer.onpostkeydown = e => { var x = SelectionStart; if (x < i1 || x > i2 || SelectionLength != 0) EndFlyer(); };
      flyer.onpostkeypress = a =>
      {
        i2 = SelectionStart; if (i2 < t2) { EndFlyer(); return; }
        var ss = text.Substring(i1, i2 - i1);
        var xx = items.Where(x => x.text.StartsWith(ss, true, null)).ToArray();
        if (xx.Length == 0) { EndFlyer(); return; }
        if (flyer.items.SequenceEqual(xx)) return;
        flyer.items = xx; flyer.Format(); flyer.select(xx[0]); flyer.Invalidate();
      };
      flyer.ontooltip = (r, item) => SetToolTip(RectangleToClient(r), t => t == 0 ? TypeHelper.tooltip(item.info) : null, 1);
    }
    void EndFlyer()
    {
      if (flyer != null) { flyer.Dispose(); flyer = null; }
    }
    protected override void OnHandleDestroyed(EventArgs e) { state = 0; map.Clear(); EndFlyer(); base.OnHandleDestroyed(e); }
    protected override void OnScroll(ScrollEventArgs se) { EndFlyer(); base.OnScroll(se); }
    protected override void OnMouseLeave(EventArgs e)
    {
      base.OnMouseLeave(e); if (ClientRectangle.Contains(PointToClient(Cursor.Position))) return;
      EndToolTip(); EndFlyer();
    }
    protected override void OnLostFocus(EventArgs e)
    {
      EndToolTip(); EndFlyer();
      if (e != null) base.OnLostFocus(e);
      //////////
      if (error != null)
      {
        Select(epos.i, epos.i + epos.n); ScrollVisible();
        //SetToolTip(TextBox(epos.i, epos.n), t => t == 0 ? $"{"Error"} {"error"}" : null);
        //node.view.MessageBox(error); // Focus();
        return;
      }
      if (state == 7) return;
      var code = EditText;
      var debug = map.Any(p => (p.v & 0x10) != 0);
      var isdebug = node.funcs != null && node.funcs.Length != 0 && node.funcs[0] == null;// node.funcs != null && ((object[])node.funcs[0]).Length == 4;
      if (debug == isdebug && code == node.getcode()) return;
      var inode = node.node;
      if (string.IsNullOrEmpty(code))
      {
        node.funcs = null;
        inode.RemoveBuffer(CDX.BUFFER.SCRIPT);
        //inode.RemoveBuffer(CDX.BUFFER.SCRIPTDATA);
        node.Invalidate(Inval.PropertySet); return;
      }
      if (node.funcs != null && node.funcs.Length != 0)
      {
        Node.SaveProps(node.node, node.GetMethod<Action<IExchange>>());
      }
      node.funcs = null;
      inode.SetBytes(CDX.BUFFER.SCRIPT, Encoding.UTF8.GetBytes(code));
      if (debug)
      {
        Script.bps = map.Where(p => p.v == 0x1A).Select(p => p.i).ToArray();
        Script.dbg = DebugStep; (Script.map = map).Clear(); sp = null; state = 0;
        node.GetMethod<Action<IExchange>>();
      }
      Node.CompactProps(node.node, node.GetMethod<Action<IExchange>>());
      node.Invalidate(Inval.PropertySet); hccode = code.GetHashCode();
      //////////
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
      if (flyer != null)
      {
        var p = Native.WindowFromPoint(PointToScreen(e.Location));
        if (p != IntPtr.Zero) { var t = Control.FromHandle(p) as TypeExplorer; if (t != null) { t.OnMouseWheel(e); return; } }
        EndFlyer();
      }
      base.OnMouseWheel(e);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
      EndToolTip(); EndFlyer(); base.OnMouseDown(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
      if (flyer != null) { flyer.OnKeyDown(e); if (e.Handled) return; } //if (e.KeyCode == Keys.Delete) askstop();
      base.OnKeyDown(e);
      if (flyer != null && flyer.onpostkeydown != null) flyer.onpostkeydown(e);
    }
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
      if (flyer != null) { flyer.OnKeyPress(e); if (e.Handled) return; }
      if (ReadOnly)
      {
        if ((ModifierKeys & (Keys.Control | Keys.Alt)) != 0) return;
        //askstop(); 
        return;
      }
      base.OnKeyPress(e);
      if (flyer != null && flyer.onpostkeypress != null) flyer.onpostkeypress(e);
    }
  }
}
