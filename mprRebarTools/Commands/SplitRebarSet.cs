namespace mprRebarTools.Commands
{
    using System;
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
    using SelectionFilters;

    /// <summary>
    /// Разделить арматурный набор на два по указанному стержню в наборе
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    //// ReSharper disable once UnusedMember.Global
    public class SplitRebarSet : IExternalCommand
    {
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CommandExecutor.Execute(() =>
            {
#if !DEBUG
                ModPlusAPI.Statistic.SendCommandStarting($"mpr{nameof(SplitRebarSet)}", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
                var doc = commandData.Application.ActiveUIDocument.Document;

                // "Выберите арматурный набор"
                var reference = commandData.Application.ActiveUIDocument.Selection
                    .PickObject(ObjectType.Element, new RebarSetSelectionFilter(), Language.GetItem("m5"));

                if (!(doc.GetElement(reference) is Rebar rebarSet))
                    throw new OperationCanceledException();

                // "Выберите точку разбивки набора"
                reference = commandData.Application.ActiveUIDocument.Selection.PickObject(
                    ObjectType.PointOnElement, Language.GetItem("m6"));

                if (reference.ElementId.IntegerValue != rebarSet.Id.IntegerValue)
                    throw new OperationCanceledException();

                var globalPoint = reference.GlobalPoint;

                Split(rebarSet, globalPoint);
            });
        }

        private void Split(Rebar rebar, XYZ point)
        {
            var maxSpacing = rebar.MaxSpacing;
#if R2017
            var distributionPath = rebar.GetDistributionPath();
#else
            var distributionPath = rebar.GetShapeDrivenAccessor().GetDistributionPath();
#endif
            var arrayLength = distributionPath.Length;
            var normal = rebar.GetRebarNormal();
            var barsOnNormalSide = Math.Abs(distributionPath.Direction.DotProduct(normal) - 1.0) < 0.0001;
            var firstCenterlineCurves =
                rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
            var startPlane =
                Plane.CreateByNormalAndOrigin(distributionPath.Direction, firstCenterlineCurves.First().GetEndPoint(0));
            var distance = point.DistanceTo(startPlane.ProjectOnto(point));
            var diameter = ((RebarBarType)rebar.Document.GetElement(rebar.GetTypeId())).GetDiameter(DiameterType.Model);
            var realStep = arrayLength / (rebar.NumberOfBarPositions - 1);

#if !R2017 && !R2018 && !R2019 && !R2020 && !R2021
            var indexesOfExcludedRebars = new System.Collections.Generic.List<int>();
            for (var i = 0; i < rebar.NumberOfBarPositions; i++)
            {
                if (!rebar.DoesBarExistAtPosition(i))
                    indexesOfExcludedRebars.Add(i);
            }
#endif
            
            var rebarConstraintsManager = rebar.GetRebarConstraintsManager();
            var sourceRebarConstraints = rebarConstraintsManager
                .GetAllConstrainedHandles()
                .Where(h => h.GetHandleType() != RebarHandleType.RebarPlane)
                .Select(rebarConstrainedHandle =>
                    rebarConstraintsManager.GetCurrentConstraintOnHandle(rebarConstrainedHandle))
                .ToList();
            
            Debug.Print($"Array length: {arrayLength.FtToMm()}");
            Debug.Print($"Real step: {realStep.FtToMm()}");
            Debug.Print($"Distance for split: {distance.FtToMm()}");

            for (var i = 0; i < rebar.NumberOfBarPositions; i++)
            {
                var lastSelected = i == rebar.NumberOfBarPositions - 1;
                var rangeStart = (realStep * i) - (diameter / 2);
                var rangeEnd = (realStep * i) + (diameter / 2);

                if (distance > rangeStart && distance < rangeEnd)
                {
                    using (var tr = new Transaction(rebar.Document, Language.GetItem("n4")))
                    {
                        tr.Start();

                        var translation = lastSelected
                            ? distributionPath.Direction * realStep * i
                            : distributionPath.Direction * realStep * (i + 1);

                        if (ElementTransformUtils.CopyElement(
                                rebar.Document, rebar.Id, translation).FirstOrDefault() is ElementId newRebarId &&
                            rebar.Document.GetElement(newRebarId) is Rebar newRebar)
                        {
                            if (rebar.LayoutRule == RebarLayoutRule.NumberWithSpacing)
                            {
                                if (lastSelected || i == rebar.NumberOfBarPositions - 2)
                                {
                                    newRebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    newRebar.NumberOfBarPositions = rebar.NumberOfBarPositions - i - 1;
                                }

                                if (i == 0)
                                {
                                    rebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    rebar.NumberOfBarPositions = lastSelected ? i : i + 1;
                                }
                            }
                            else if (rebar.LayoutRule == RebarLayoutRule.FixedNumber)
                            {
                                if (lastSelected || i == rebar.NumberOfBarPositions - 2)
                                {
                                    newRebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    var newArrayLength = arrayLength - (realStep * (i + 1));
                                    newRebar.SetLayoutAsFixedNumber(
                                      rebar.NumberOfBarPositions - i - 1,
                                      newArrayLength,
                                      barsOnNormalSide,
                                      includeLastBar: rebar.IncludeLastBar);
                                    SetArrayLength(newRebar, newArrayLength);
                                }

                                if (i == 0)
                                {
                                    rebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    var newArrayLength = lastSelected ? realStep * (i - 1) : realStep * i;
                                    rebar.SetLayoutAsFixedNumber(
                                        lastSelected ? i : i + 1,
                                        newArrayLength,
                                        barsOnNormalSide,
                                        rebar.IncludeFirstBar);
                                    SetArrayLength(rebar, newArrayLength);
                                }
                            }
                            else if (rebar.LayoutRule == RebarLayoutRule.MaximumSpacing)
                            {
                                if (lastSelected || i == rebar.NumberOfBarPositions - 2)
                                {
                                    newRebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    var newArrayLength = arrayLength - (realStep * (i + 1));
                                    newRebar.SetLayoutAsMaximumSpacing(
                                      maxSpacing,
                                      newArrayLength,
                                      barsOnNormalSide,
                                      includeLastBar: rebar.IncludeLastBar);
                                    SetArrayLength(newRebar, newArrayLength);
                                }

                                if (i == 0)
                                {
                                    rebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    var newArrayLength = lastSelected ? realStep * (i - 1) : realStep * i;
                                    rebar.SetLayoutAsMaximumSpacing(
                                      maxSpacing,
                                      newArrayLength,
                                      barsOnNormalSide,
                                      rebar.IncludeFirstBar);
                                    SetArrayLength(rebar, newArrayLength);
                                }
                            }
                            else if (rebar.LayoutRule == RebarLayoutRule.MinimumClearSpacing)
                            {
                                if (lastSelected || i == rebar.NumberOfBarPositions - 2)
                                {
                                    newRebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    var newArrayLength = arrayLength - (realStep * (i + 1));
                                    newRebar.SetLayoutAsMinimumClearSpacing(
                                        maxSpacing,
                                        newArrayLength,
                                        barsOnNormalSide,
                                        includeLastBar: rebar.IncludeLastBar);
                                    SetArrayLength(newRebar, newArrayLength);
                                }

                                if (i == 0)
                                {
                                    rebar.SetLayoutAsSingle();
                                }
                                else
                                {
                                    var newArrayLength = lastSelected ? realStep * (i - 1) : realStep * i;
                                    rebar.SetLayoutAsMinimumClearSpacing(
                                        maxSpacing,
                                        newArrayLength,
                                        barsOnNormalSide,
                                        rebar.IncludeFirstBar);
                                    SetArrayLength(rebar, newArrayLength);
                                }
                            }
                            
                            if (rebar.DistributionType == DistributionType.VaryingLength)
                            {
                                var newRebarConstraintsManager = newRebar.GetRebarConstraintsManager();

                                var index = -1;
                                foreach (var rebarConstrainedHandle in newRebarConstraintsManager.GetAllConstrainedHandles())
                                {
                                    var rebarHandleType = rebarConstrainedHandle.GetHandleType();
                                    if (rebarHandleType == RebarHandleType.RebarPlane)
                                        continue;

                                    index++;

                                    var sourceRebarConstraint = sourceRebarConstraints[index];
                                    newRebarConstraintsManager.SetPreferredConstraintForHandle(rebarConstrainedHandle, sourceRebarConstraint);
                                }
                            }

#if !R2017 && !R2018 && !R2019 && !R2020 && !R2021
                            if (indexesOfExcludedRebars.Any())
                            {
                                for (var k = 0; k < newRebar.NumberOfBarPositions; k++)
                                {
                                    newRebar.SetBarIncluded(true, k);
                                }

                                for (var k = 0; k < indexesOfExcludedRebars.Count; k++)
                                {
                                    indexesOfExcludedRebars[k] = indexesOfExcludedRebars[k] - i - 1;
                                }

                                foreach (var k in indexesOfExcludedRebars.Where(index => index >= 0))
                                {
                                    newRebar.SetBarIncluded(false, k);
                                }
                            }
#endif
                        }

                        tr.Commit();
                    }

                    break;
                }
            }
        }

        private void SetArrayLength(Rebar rebar, double arrayLength)
        {
#if R2017
            rebar.ArrayLength = arrayLength;
#else
            rebar.GetShapeDrivenAccessor().ArrayLength = arrayLength;
#endif
        }
    }
}
