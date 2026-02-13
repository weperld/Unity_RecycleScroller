namespace RecycleScroll
{
    public interface IRecycleScrollbarDelegate
    {
        public bool LoopScrollIsOn { get; }
        public bool IsLoopScrollable { get; }
        public float RealSize { get; }
        public float ShowingSize { get; }
        public float ViewportSize { get; }
        
        public float ConvertRealToShow(float real);
        public float ConvertShowToReal(float show);
    }
}