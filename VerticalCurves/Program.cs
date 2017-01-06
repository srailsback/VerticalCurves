using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerticalCurves
{
    class Program
    {
        static void Main(string[] args)
        {
            ICurvesRepository _repo = new CurvesRepository();


            // truncate VerticalCurves destination table
            _repo.TruncateVerticalCurves();

            /* rip through VerticalCurves source and update the destination
                insert gaps for start - routes that don't start at 0 FROMMEAS
                insert records for gaps in the route, the previous TOMEAS does not equal the next FROMMEAS, use the previous TOMEAS for the new FROMMEAS and next FROMMEAS for new TOMEAS
                insert records for gaps at end of route, the last TOMEAS does not equal the RouteLength, use the last TOMEAS for new FROMMEAS and RunLength for new TOMEAS
            */

            // get all run lengths
            var routes = _repo.GetAllRouteLengths().Take(100);

            #region insert vertical curves

            foreach(var route in routes)
            {
                var curves = _repo.GetVerticalCurvesByRoute(route.ROUTE, true);

                var lastIndex = curves.Count() - 1;

                foreach (var curve in curves)
                {
                    // start at the beginning, if route does not start with 0 FROMMEAS insert it.
                    var index = curves.IndexOf(curve);
                    if (index == 0)
                    {
                        if (curve.FROMMEAS > 0)
                        {
                            _repo.InsertRouteStart(curve);
                            _repo.InsertVerticalCurve(curve);
                        }
                        else
                        {
                            _repo.InsertVerticalCurve(curve);
                        }
                    }

                    if (index > 0)
                    {
                        // now figure out gaps in route
                        var previous = curves.Skip(curves.IndexOf(curve) - 1).Take(1).FirstOrDefault();
                        var next = curves.Skip(curves.IndexOf(curve) + 1).Take(1).FirstOrDefault();

                        if (curve.FROMMEAS != previous.TOMEAS)
                        {
                            _repo.InsertRouteGap(previous, curve);
                            _repo.InsertVerticalCurve(curve);
                        }
                        else
                        {
                            _repo.InsertVerticalCurve(curve);
                        }
                    }


                    // check end of route, the TOMEAS should equal the RouteLength.LENGTH
                    if (index == lastIndex && curve.TOMEAS != route.LENGTH)
                    {
                        _repo.InsertRouteEnd(curve, route);
                    }
                }

            }
            #endregion
             

            #region update vertical curves
            foreach(var route in routes)
            {
                var curves = _repo.GetVerticalCurvesByRoute(route.ROUTE, false);
                foreach(var curve in curves)
                {
                    var index = curves.IndexOf(curve);
                    var lastIndex = curves.IndexOf(curves.Last());

                    decimal prevPERCENTGRA;
                    decimal nextPERCENTGRA;
                    decimal currPERCENTGRA = curve.PERCENTGRA;

                    if (index == 0)
                    {
                        prevPERCENTGRA = 0;
                        nextPERCENTGRA = curves.Skip(curves.IndexOf(curve) + 1).First().PERCENTGRA;
                    } else if (index == lastIndex)
                    {
                        prevPERCENTGRA = curves.Skip(curves.IndexOf(curve) - 1).First().PERCENTGRA;
                        nextPERCENTGRA = 0;
                    } else
                    {
                        prevPERCENTGRA = curves.Skip(curves.IndexOf(curve) - 1).First().PERCENTGRA;
                        nextPERCENTGRA = curves.Skip(curves.IndexOf(curve) + 1).First().PERCENTGRA;
                    }

                    // for first and last records, if distance is greater than 528 ft, VAFT should be an empty string
                    if ((index == 0 || index == lastIndex) && curve.DISTANCE > 528)
                    {
                        curve.VAFT = "";
                    }
                    else
                    {

                        if (nextPERCENTGRA == 0 && prevPERCENTGRA == 0)
                        {
                            curve.VAFT = VAFT.VG.ToString();
                        }
                        else if (prevPERCENTGRA < nextPERCENTGRA)
                        {
                            curve.VAFT = VAFT.SVC.ToString();
                        }
                        else
                        {
                            curve.VAFT = VAFT.CVC.ToString();
                        }

                    }
                    _repo.UpdateVerticalCurve(curve);
                }
            }

            #endregion


            Console.WriteLine("Done");
            Console.ReadKey();
        }


    }

    public enum VAFT
    {
        CVC,
        SVC,
        /*
          previous and next PERCENTGRA are 0
          OR previous and next PECENTGRA are negative
          OR previous and next PECENTGRA are positive
          OR previous PR and next PECENTGRA are positive
        */
        VG
    }
}
