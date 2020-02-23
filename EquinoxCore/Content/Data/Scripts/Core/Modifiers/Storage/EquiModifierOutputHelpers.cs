using System;
using Equinox76561198048419394.Core.Debug;
using Equinox76561198048419394.Core.ModelGenerator;
using Equinox76561198048419394.Core.Modifiers.Def;
using Sandbox.ModAPI;
using VRage.Components;
using VRage.Components.Entity.CubeGrid;
using VRage.Entity.Block;
using VRage.Entity.EntityComponents;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.Models;
using VRage.Logging;
using VRage.Session;
using VRageMath;
using VRageRender;

namespace Equinox76561198048419394.Core.Modifiers.Storage
{
    public static class EquiModifierOutputHelpers
    {
        private static bool IsDedicated => ((IMyUtilities) MyAPIUtilities.Static).IsDedicated;

        public static void Apply(in ModifierOutput modifier, MyEntity target)
        {
            try
            {
                var modelComp = target.Get<MyModelComponent>();
                var render = target.Get<MyRenderComponentBase>();
                if (modelComp == null)
                    return;

                var model = modifier.Model;
                if (modifier.MaterialEditsBuilder != null && !IsDedicated)
                    model = MySession.Static.Components.Get<DerivedModelManager>().CreateModel(model, modifier.MaterialEditsBuilder);

                var modelData = MyModels.GetModelOnlyData(model);
                if (modelData != null && modelComp.Model != modelData)
                {
                    modelComp.SetModel(modelData);
                    if (render?.RenderObjectIDs != null && render.RenderObjectIDs.Length > 0)
                    {
                        foreach (var renderObj in render.RenderObjectIDs)
                            MyRenderProxy.UpdateHighlightOverlappingModel(renderObj, false);
                        render.RemoveRenderObjects();
                        render.AddRenderObjects();
                        foreach (var renderObj in render.RenderObjectIDs)
                        {
                            MyRenderProxy.UpdateHighlightOverlappingModel(renderObj, false);
                            MyRenderProxy.UpdateLodImmediately(renderObj);
                        }
                    }
                }

                if (IsDedicated || render == null)
                    return;
                if (modifier.ColorMaskHsv.HasValue)
                {
                    render.EnableColorMaskHsv = true;
                    modelComp.ColorMask = modifier.ColorMaskHsv.Value;
                }
                else
                {
                    render.EnableColorMaskHsv = false;
                    modelComp.ColorMask = Vector3.Zero;
                }

                render?.UpdateColorMask(modelComp.ColorMask);
            }
            catch (Exception ex)
            {
                DebugFlags.MaybeFailFast(nameof(EquiModifierOutputHelpers), $"Failed to apply modifiers to {target}.\nModifiers: {modifier}", ex);
            }
        }

        public static void Apply(in ModifierOutput modifier, MyBlock block, MyGridDataComponent gridData, MyRenderComponentGrid gridRender)
        {
            try
            {
                var model = modifier.Model;
                if (modifier.MaterialEditsBuilder != null && !((IMyUtilities) MyAPIUtilities.Static).IsDedicated)
                    model = MySession.Static.Components.Get<DerivedModelManager>().CreateModel(model, modifier.MaterialEditsBuilder);

                var modelData = MyModels.GetModelOnlyData(model);
                if (modelData != null && block.Model != modelData)
                    gridData.ChangeModel(block, modelData);

                if (IsDedicated || gridRender == null)
                    return;
                var colorMaskHsv = modifier.ColorMaskHsv ?? Vector3.Zero;
                foreach (var renderable in gridRender.GetBlockRenderObjectIDs(block.Id))
                    MyRenderProxy.UpdateRenderEntity(renderable, null, colorMaskHsv);
            }
            catch (Exception ex)
            {
                DebugFlags.MaybeFailFast(nameof(EquiModifierOutputHelpers),
                    $"Failed to apply modifiers to {block} on {gridData.Entity}.\nModifiers: {modifier}", ex);
            }
        }
    }
}