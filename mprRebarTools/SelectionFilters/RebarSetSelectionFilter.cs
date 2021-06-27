namespace mprRebarTools.SelectionFilters
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Фильтр выбора арматурного набора
    /// </summary>
    public class RebarSetSelectionFilter : ISelectionFilter
    {
        /// <inheritdoc/>
        public bool AllowElement(Element elem)
        {
            return elem is Rebar rebar &&
#if R2017
                   rebar.LayoutRule != RebarLayoutRule.Single;
#else
                   rebar.LayoutRule != RebarLayoutRule.Single &&
                   rebar.IsRebarShapeDriven();
#endif
        }

        /// <inheritdoc/>
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new System.NotImplementedException();
        }
    }
}
