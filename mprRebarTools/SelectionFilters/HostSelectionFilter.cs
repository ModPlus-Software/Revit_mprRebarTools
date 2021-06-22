namespace mprRebarTools.SelectionFilters
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Фильтр выбора основы
    /// </summary>
    public class HostSelectionFilter : ISelectionFilter
    {
        /// <inheritdoc />
        public bool AllowElement(Element elem)
        {
            return elem.Category != null &&
                   (elem is HostObject ||
                    elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns ||
                    elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation ||
                    elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming ||
                    elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_EdgeSlab ||
                    elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Stairs);
        }

        /// <inheritdoc />
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new System.NotImplementedException();
        }
    }
}
