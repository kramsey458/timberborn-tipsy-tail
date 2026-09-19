using System;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockObjectModelSystem;
using Timberborn.BlockSystem;
using Timberborn.BlueprintSystem;
using Timberborn.EntitySystem;
using Timberborn.TemplateInstantiation;
using Timberborn.TerrainSystem;
using UnityEngine;

namespace Kyler.TipsyTail
{
    public record TipsyTailTerrainCutoutSpec : ComponentSpec
    {
        [Serialize] public ImmutableArray<Vector3Int> CutoutTiles { get; init; }
    }

    // Deliberately NOT ICutoutTilesProvider: native placement previews must not
    // acquire a second, independently managed cutout for this building.
    public sealed class TipsyTailTerrainCutout : BaseComponent, IAwakableComponent,
        IInitializableEntity, IPostInitializableEntity, IDeletableEntity,
        IPrePlacementChangeListener, IPostPlacementChangeListener
    {
        private readonly ITerrainService _terrain;
        private BlockObject _block;
        private BlockObjectModelController _model;
        private EntityComponent _entity;
        private TipsyTailTerrainCutoutSpec _spec;
        private CutoutLease<Vector3Int> _lease;
        private bool _initialized;
        private bool _moving;
        private bool _deleted;

        public TipsyTailTerrainCutout(ITerrainService terrain) { _terrain = terrain; }

        public void Awake()
        {
            _block = GetComponent<BlockObject>();
            _model = GetComponent<BlockObjectModelController>();
            _entity = GetComponent<EntityComponent>();
            _spec = GetComponent<TipsyTailTerrainCutoutSpec>();
            if (_spec.CutoutTiles.IsDefaultOrEmpty)
                throw new InvalidOperationException("Tipsy Tail requires terrain cutout tiles.");
            _lease = new CutoutLease<Vector3Int>(_terrain.SetCutout, _terrain.UnsetCutout);
        }

        public void InitializeEntity()
        {
            if (_initialized || _deleted) return;
            _initialized = true;
            _model.ModelsUpdated += OnModelsUpdated;
            UpdateCutout();
        }

        public void PostInitializeEntity() { UpdateCutout(); }
        public void OnPrePlacementChanged() { _moving = true; _lease.Hide(); }
        public void OnPostPlacementChanged() { _moving = false; UpdateCutout(); }

        public void DeleteEntity()
        {
            if (_deleted) return;
            _deleted = true;
            if (_initialized) _model.ModelsUpdated -= OnModelsUpdated;
            // Release the cached world tiles, even if placement/model state changed.
            _lease.Dispose();
        }

        private void OnModelsUpdated(object sender, EventArgs args) { UpdateCutout(); }

        private void UpdateCutout()
        {
            if (_deleted || (_entity != null && _entity.Deleted))
            {
                DeleteEntity();
                return;
            }
            if (_initialized && !_moving && !_block.IsPreview && _block.IsFinished
                && _model.IsFinishedModelShown)
                _lease.Show(_spec.CutoutTiles.Select(tile => _block.TransformCoordinates(tile)));
            else
                _lease.Hide();
        }
    }

    [Context("Game")]
    [Context("MapEditor")]
    public sealed class TipsyTailTerrainConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<TipsyTailTerrainCutout>().AsTransient();
            MultiBind<TemplateModule>().ToProvider(CreateModule).AsSingleton();
        }

        private static TemplateModule CreateModule()
        {
            var builder = new TemplateModule.Builder();
            builder.AddDecorator<TipsyTailTerrainCutoutSpec, TipsyTailTerrainCutout>();
            return builder.Build();
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
