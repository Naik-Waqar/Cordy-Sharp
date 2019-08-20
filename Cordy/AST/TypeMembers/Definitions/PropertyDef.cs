namespace Cordy.AST
{
    /// <summary>
    /// Property definition
    /// </summary>
    public sealed class PropertyDef : Definition
    {
        public PropertyDef(eAccessLevel lvl, bool isProtected, bool isStatic, TypeNode type, string name)
            : base(lvl, isProtected, isStatic, type, null, name)
        { }
    }
}
