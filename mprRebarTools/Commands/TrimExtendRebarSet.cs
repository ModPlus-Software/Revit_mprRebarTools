namespace mprRebarTools.Commands;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ModPlus_Revit.Enums;
using ModPlus_Revit.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using SelectionFilters;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class TrimExtendRebarSet : IExternalCommand
{
    /// <inheritdoc />
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) => CommandExecutor.Execute(() =>
    {
#if !DEBUG
        ModPlusAPI.Statistic.SendCommandStarting($"mpr{nameof(TrimExtendRebarSet)}", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
        var doc = commandData.Application.ActiveUIDocument.Document;

        // "Выберите арматурный набор"
        var reference = commandData.Application.ActiveUIDocument.Selection
            .PickObject(ObjectType.Element, new RebarSetSelectionFilter(), Language.GetItem("m5"));

        if (doc.GetElement(reference) is not Rebar rebarSet)
            throw new OperationCanceledException();

        // todo "Выберите точку разбивки набора"
        reference = commandData.Application.ActiveUIDocument.Selection.PickObject(
            ObjectType.Face, Language.GetItem("m6"));

        if (doc.GetElement(reference).GetGeometryObjectFromReference(reference) is not Face face)
            throw new OperationCanceledException();

        //RevitGeometryExporter.ExportGeometryToXml.ExportWallByFaces((Wall)doc.GetElement(reference), "wall");

        // todo get offset
        var offset = 20.MmToFt();

        TrimExtend(rebarSet, face, offset);
    });

    private void TrimExtend(Rebar rebar, Face face, double offset)
    {
        using (var tr = new TransactionGroup(rebar.Document, "Trim extend"))
        {
            tr.Start();

#if R2017
            var distributionPath = rebar.GetDistributionPath();
#else
            var distributionPath = rebar.GetShapeDrivenAccessor().GetDistributionPath();
#endif
            var arrayLength = distributionPath.Length;
            var normal = rebar.GetRebarNormal();
            var barsOnNormalSide = Math.Abs(distributionPath.Direction.DotProduct(normal) - 1.0) < 0.0001;
            var firstCenterlineCurves = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
            var diameter = ((RebarBarType)rebar.Document.GetElement(rebar.GetTypeId())).GetDiameter(DiameterType.Model);
            var realStep = arrayLength / (rebar.NumberOfBarPositions - 1);
            
            var splitPoints = new List<XYZ>();

            var previousHasIntersection = false;
            for (var i = 0; i < rebar.NumberOfBarPositions; i++)
            {
                if (!rebar.IncludeFirstBar && i == 0)
                    continue;
                if (!rebar.IncludeLastBar && i == rebar.NumberOfBarPositions - 1)
                    continue;

#if !R2017 && !R2018 && !R2019 && !R2020 && !R2021
                if (!rebar.DoesBarExistAtPosition(i))
                    continue;
#endif
                var translation = distributionPath.Direction * realStep * i;
                var transform = Transform.CreateTranslation(translation);
                var lines = new List<Line>();
                foreach (var curve in firstCenterlineCurves)
                {
                    if (curve.CreateTransformed(transform) is Line line)
                        lines.Add(line);
                }

                var hasIntersection = false;
                foreach (var line in lines)
                {
                    var pt = line.GetEndPoint(0);
                    var checkLine = Line.CreateBound(pt - (line.Direction * 1000), pt + (line.Direction * 1000));
                    if (face.Intersect(checkLine, out var intersectionResultArray) == SetComparisonResult.Overlap &&
                        intersectionResultArray is { Size: 1 })
                    {
                        hasIntersection = true;
                        break;
                    }
                }

                if (i == 0)
                {
                    previousHasIntersection = hasIntersection;
                }
                else
                {
                    if (previousHasIntersection != hasIntersection)
                    {
                        // Перепад на предыдущем шаге!
                        splitPoints.Add(firstCenterlineCurves.First().GetEndPoint(0) + (distributionPath.Direction * realStep * (i - 1)));
                    }

                    previousHasIntersection = hasIntersection;
                }
            }

            if (!splitPoints.Any())
            {
                tr.RollBack();
                return;
            }

            ////RevitGeometryExporter.ExportGeometryToXml.ExportFace(face, "face");
            ////RevitGeometryExporter.ExportGeometryToXml.ExportPoints(splitPoints, nameof(splitPoints));

            //MessageBox.Show(splitPoints.Count.ToString());
            var rebars = SplitRebarSet.Split(rebar, splitPoints);

            tr.Assimilate();
        }
    }
}