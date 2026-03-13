using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_RenderSetDirty : CompProperties
    {
        public CompProperties_RenderSetDirty()
        {
            this.compClass = typeof(CompRenderSetDirty);
        }
    }
    public class CompRenderSetDirty : ThingComp
    {
        public CompProperties_RenderSetDirty Props
        {
            get
            {
                return (CompProperties_RenderSetDirty)this.props;
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            pawn.Drawer.renderer.renderTree.SetDirty();
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            pawn.Drawer.renderer.renderTree.SetDirty();
        }
    }
}
