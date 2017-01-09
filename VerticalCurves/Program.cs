using System;
using System.Linq;

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
            var routes = _repo.GetAllRouteLengths();

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
                        /* if the distance of the route start is greater than 528 insert two records
                        * 1st, FROMEAS is 0, TOMEAS is the next's FROMMEAS  
                        * 2nd, FROMMEAS is current curve record's TOMEAS - 528 and TOMEAS is its FROMEAS
                        * the InsertRouteStart contains this logic
                        */
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
                    if (index == lastIndex)
                    {
                        /*
                         * if distance is greater than 528 ft, insert two records
                         * 1st, the FROMMEAS is the previous TOMEAS, and TOMEAS is FROMMEAS + 528
                         * 2nd, FROMMEAS is 1st record's TOMEAS and TOMEAS is ROUTE LENGTH
                         * our InsertRouteEnd wraps this logic
                         */
                        if (curve.TOMEAS != route.LENGTH)
                        {
                            _repo.InsertRouteEnd(curve, route);
                        }
                    }                  
                }
            }
            #endregion


            #region update vertical curves
            _repo.ResetRowCount();
            foreach (var route in routes)
            {
                var curves = _repo.GetVerticalCurvesByRoute(route.ROUTE, false);
                foreach (var current in curves)
                {
                    var index = curves.IndexOf(current);
                    var lastIndex = curves.IndexOf(curves.Last());

                    // previous and next curve records
                    VerticalCurve previous = null;
                    VerticalCurve next = null;
                    if (index == 0)
                    {
                        next = curves.Skip(curves.IndexOf(current) + 1).First();
                    }
                    else if (index == lastIndex)
                    {
                        previous = curves.Skip(curves.IndexOf(current) - 1).First();
                    }
                    else
                    {
                        previous = curves.Skip(curves.IndexOf(current) - 1).First();
                        next = curves.Skip(curves.IndexOf(current) + 1).First();
                    }

                    // if this it start or end of the route skip it
                    if (index > 0 && index < lastIndex)
                    {
                        // update the VAFT
                        /*
                        + 0 + CVC
                        - 0 - SVC
                        - 0 + SVC
                        + 0 - CVC
                        0 0 - CVC
                        0 0 + SVC
                        - 0 0 SCV
                        + 0 0 CVC
                        if current PERCENTGRA == 0 figure out the VAFT
                        if current PERCENTGRA != 0, it's VG
                        */
                        if (current.PERCENTGRA != 0)
                        {
                            current.VAFT = VAFT.VG.ToString();
                            _repo.UpdateVerticalCurve(current);
                        }
                        else
                        {
                            if (previous.PERCENTGRA > 0 && next.PERCENTGRA > 0)
                            {
                                current.VAFT = VAFT.CVC.ToString();
                            }
                            else if (previous.PERCENTGRA < 0 && next.PERCENTGRA < 0)
                            {
                                current.VAFT = VAFT.SVC.ToString();
                            }
                            else if (previous.PERCENTGRA < 0 && next.PERCENTGRA > 0)
                            {
                                current.VAFT = VAFT.SVC.ToString();
                            }
                            else if (previous.PERCENTGRA > 0 && next.PERCENTGRA < 0)
                            {
                                current.VAFT = VAFT.CVC.ToString();
                            }
                            else if (previous.PERCENTGRA == 0 && next.PERCENTGRA < 0)
                            {
                                current.VAFT = VAFT.CVC.ToString();
                            }
                            else if (previous.PERCENTGRA == 0 && next.PERCENTGRA > 0)
                            {
                                current.VAFT = VAFT.SVC.ToString();
                            }
                            else if (previous.PERCENTGRA < 0 && next.PERCENTGRA == 0)
                            {
                                current.VAFT = VAFT.SVC.ToString();
                            }
                            else if (previous.PERCENTGRA > 0 && next.PERCENTGRA == 0)
                            {
                                current.VAFT = VAFT.CVC.ToString();
                            }
                            else if (previous.PERCENTGRA == 0 && next.PERCENTGRA == 0 && current.PERCENTGRA != 0)
                            {
                                current.VAFT = VAFT.VG.ToString();
                            } 

                            if (current.VAFT != "")
                            {
                                _repo.UpdateVerticalCurve(current);
                            }
                        }
                    }


                    // case for the last record
                    if (index == lastIndex && current.PERCENTGRA <= 528)
                    {

                        if (current.PERCENTGRA != 0) {
                            current.VAFT = VAFT.VG.ToString();
                            _repo.UpdateVerticalCurve(current);
                        }
                        else
                        {
                            current.VAFT = previous.PERCENTGRA < 0 ? VAFT.SVC.ToString() : VAFT.CVC.ToString();
                            _repo.UpdateVerticalCurve(current);
                        }
                    }
                }
            }

            #endregion


            Console.WriteLine("Done");
            //Console.ReadKey();
        }
    }
}
