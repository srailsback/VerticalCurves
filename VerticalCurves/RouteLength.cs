namespace VerticalCurves
{
    public class RouteLength
    {
        public string ROUTE;
        public decimal LENGTH;

        public RouteLength() { }
        public RouteLength(dynamic d) 
        {
            ROUTE = d.ROUTE;
            LENGTH = decimal.Parse(d.LENGTH.ToString());
        }
    }
}
