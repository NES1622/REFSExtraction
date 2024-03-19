namespace EligibleAutoRefernces
{
    internal class RefsRecord
    {
        public int PageNumber { get; set; }
        public double XValue { get; set; }
        public double YValue { get; set; }
        public string Reference { get; set; }
        public string ID { get; set; }
    }

    public class REFsGeoPoints
    {
        public string Key { get; set; }
        public string ID { get; set; }
        public int PageNumber { get; set; }
        public int ColumnNumber { get; set; }
        public double XBeginPoint { get; set; }
    }
    internal class RefsDataMatric
    {
        public string fID { get; set; }
        public int PageNumber { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public double fontSize { get; set; }
        public double XValue { get; set; }
        public double YValue { get; set; }
        public string Data { get; set; }
        public string DataType { get; set; }
        public string XCode { get; set; }
        public string YCode { get; set; }
        public string ID { get; set; }
        public double XDistHead { get; set; }
        public double YDistHead { get; set; }
        public double XDistPrev { get; set; }
        public double YDistPrev { get; set;}
    }
}
