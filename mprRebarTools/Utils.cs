namespace mprRebarTools;

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

/// <summary>
/// Утилиты
/// </summary>
public static class Utils
{
    /// <summary>
    /// Возвращает элементы армирования из элемента-основы
    /// </summary>
    /// <param name="host">Элемент-основа</param>
    /// <param name="getRebar">Get <see cref="Rebar"/></param>
    /// <param name="getAreaReinforcement">Get <see cref="AreaReinforcement"/></param>
    /// <param name="getPathReinforcement">Get <see cref="PathReinforcement"/></param>
    /// <param name="gerRebarContainer">Get <see cref="RebarContainer"/></param>
    public static IEnumerable<Element> GetReinforcement(
        Element host,
        bool getRebar = true,
        bool getAreaReinforcement = true,
        bool getPathReinforcement = true,
        bool gerRebarContainer = true)
    {
        var rebarHostData = RebarHostData.GetRebarHostData(host);

        if (rebarHostData == null)
            yield break;

        if (getAreaReinforcement)
        {
            foreach (var areaReinforcement in rebarHostData.GetAreaReinforcementsInHost())
            {
                yield return areaReinforcement;
            }
        }

        if (getPathReinforcement)
        {
            foreach (var pathReinforcement in rebarHostData.GetPathReinforcementsInHost())
            {
                yield return pathReinforcement;
            }
        }

        if (gerRebarContainer)
        {
            foreach (var rebarContainer in rebarHostData.GetRebarContainersInHost())
            {
                yield return rebarContainer;
            }
        }

        if (getRebar)
        {
            foreach (var rebar in rebarHostData.GetRebarsInHost())
            {
                yield return rebar;
            }
        }
    }
}