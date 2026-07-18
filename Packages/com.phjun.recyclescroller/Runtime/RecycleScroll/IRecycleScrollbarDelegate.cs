namespace RecycleScroll
{
    public interface IRecycleScrollbarDelegate
    {
        public bool IsLoopScrollable { get; }
        public float ContentSize { get; }
        public float ViewportSize { get; }
    }
}
