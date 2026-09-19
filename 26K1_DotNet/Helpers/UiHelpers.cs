namespace _26K1_DotNet
{
    /// <summary>
    /// ComboBox display item for semester selection.
    /// Consolidated from PanelTuition, PanelStatistics, FormTuitionDetail, and FormBatchTuition.
    /// </summary>
    public class SemItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public SemItem() { }

        public SemItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString() => Name;
    }
}
