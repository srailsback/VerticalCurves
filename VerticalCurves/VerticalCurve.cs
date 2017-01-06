﻿namespace VerticalCurves
{
    public class VerticalCurve
    {
        public string ROUTE;
        public decimal FROMMEAS;
        public decimal TOMEAS;
        public decimal DISTANCE;
        public decimal PERCENTGRA;
        public string VAFT;

        public VerticalCurve() { }

        public VerticalCurve(dynamic d)
        {
            ROUTE = d.ROUTE;
            FROMMEAS = decimal.Parse(d.FROMMEAS.ToString());
            TOMEAS = decimal.Parse(d.TOMEAS.ToString());
            DISTANCE = decimal.Parse(d.DISTANCE.ToString());
            PERCENTGRA = decimal.Parse(d.PERCENTGRA.ToString());
            VAFT = d.VAFT;
        }
    }
}
