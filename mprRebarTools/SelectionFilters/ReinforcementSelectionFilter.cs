namespace mprRebarTools.SelectionFilters;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

/// <inheritdoc />
public class ReinforcementSelectionFilter : ISelectionFilter
{
    /// <inheritdoc />
    public bool AllowElement(Element e)
    {
        return IsAllowableElement(e);
    }

    /// <inheritdoc />
    public bool AllowReference(Reference r, XYZ p)
    {
        return false;
    }

    /// <summary>
    /// явл€етс€ ли элемент подход€щим элементом-основой
    /// </summary>
    /// <param name="e">Ёлемент</param>
    public static bool IsAllowableElement(Element e)
    {
        return e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Rebar ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_EdgeSlab ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Stairs ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PathRein ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_AreaRein ||
#if !R2017 && !R2018 && !R2019 && !R2020
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FabricReinforcement ||
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FabricAreas ||
#endif
               e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel;
    }
}