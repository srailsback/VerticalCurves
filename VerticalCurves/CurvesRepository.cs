using Massive;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;



namespace VerticalCurves
{

    public enum VAFT
    {
        CVC,
        SVC,
        VG
    }

    public interface ICurvesRepository
    {
        List<RouteLength> GetAllRouteLengths();

        List<VerticalCurve> GetAllVerticalCurves(bool isSource);

        List<VerticalCurve> GetVerticalCurvesByRoute(string route, bool isSource);

        void InsertVerticalCurve(VerticalCurve curve);

        void UpdateVerticalCurve(VerticalCurve source, VerticalCurve toUpdate);

        void InsertRouteStart(VerticalCurve curve);

        void InsertRouteEnd(VerticalCurve curve, RouteLength route);

        void InsertRouteGap(VerticalCurve previousCurve, VerticalCurve currentCurve);

        void TruncateVerticalCurves();

        void ResetRowCount();

        void DeleteVerticalCurve(VerticalCurve curve);

    }

    public class CurvesRepository : ICurvesRepository
    {

        private int rowCount = 0;

        public CurvesRepository()
        {
        }


        public void ResetRowCount()
        {
            rowCount = 0;
        }

        public List<RouteLength> GetAllRouteLengths()
        {
            var runLengths = new RouteLengths().All().Select(x => new RouteLength(x)).ToList();
            return runLengths;
        }

        public List<VerticalCurve> GetAllVerticalCurves(bool isSource)
        {
            IEnumerable<dynamic> verticalCurves = null;
            if (isSource)
            {
                verticalCurves = new VerticalCurvesSource().All();
            }
            else
            {
                verticalCurves = new VerticalCurves().All();
            }
            return verticalCurves.Select(x => new VerticalCurve(x)).ToList();
        }

        public List<VerticalCurve> GetVerticalCurvesByRoute(string route, bool isSource)
        {
            IEnumerable<dynamic> verticalCurves = null;
            var args = new List<object>();
            args.Add(route);
            if (isSource)
            {
                verticalCurves = new VerticalCurvesSource().All(where: "ROUTE = @0", args: args.ToArray());

            }
            else
            {
                verticalCurves = new VerticalCurves().All(where: "ROUTE = @0", args: args.ToArray());
            }

            return verticalCurves.Select(x => new VerticalCurve(x)).ToList();
        }

        public void InsertVerticalCurve(VerticalCurve curve)
        {
            var query = "INSERT INTO VerticalCurves (ROUTE, FROMMEAS, TOMEAS, DISTANCE, PERCENTGRA, VAFT) VALUES (@0, @1, @2, @3, @4, @5)";
            var args = new List<object>();
            args.Add(curve.ROUTE);
            args.Add(curve.FROMMEAS);
            args.Add(curve.TOMEAS);
            args.Add(curve.DISTANCE);
            args.Add(curve.PERCENTGRA);
            args.Add(curve.VAFT);
            new VerticalCurves().Execute(query, args: args.ToArray());
            var str = string.Format("RowCount: {0},INSERTED ROUTE: {1}, FROMMEAS: {2}, TOMEAS: {3}", rowCount = rowCount + 1, curve.ROUTE, curve.FROMMEAS, curve.TOMEAS);
            Console.WriteLine(str);

        }

        public void UpdateVerticalCurve(VerticalCurve source, VerticalCurve toUpdate)
        {
            var query = "UPDATE VerticalCurves SET FROMMEAS = @0, TOMEAS = @1, DISTANCE = @2, PERCENTGRA = @3, VAFT = @4 WHERE ROUTE = @5 AND FROMMEAS = @6 AND TOMEAS = @7";
            var args = new List<object>();
            args.Add(toUpdate.FROMMEAS); // 0
            args.Add(toUpdate.TOMEAS); // 1
            args.Add(toUpdate.DISTANCE); // 2
            args.Add(toUpdate.PERCENTGRA); // 3
            args.Add(toUpdate.VAFT); // 4
            args.Add(source.ROUTE); // 5
            args.Add(source.FROMMEAS); // 6
            args.Add(source.TOMEAS); // 7
            var table = new VerticalCurves();
            table.Execute(query, args: args.ToArray());
            var str = string.Format("RowCount: {0}, UPDATED ROUTE: {1}, FROMMEAS: {2}, TOMEAS: {3}, VAFT: {4}", rowCount = rowCount + 1, toUpdate.ROUTE, toUpdate.FROMMEAS, toUpdate.TOMEAS, toUpdate.VAFT);
            Console.WriteLine(str);
        }

        public void InsertRouteStart(VerticalCurve curve)
        {
            /* if the distance of the route start is greater than 528 insert two records
            * 1st, FROMEAS is 0, TOMEAS is the next's FROMMEAS  
            * 2nd, FROMMEAS is current curve record's TOMEAS - 528 and TOMEAS is its FROMEAS
            * the InsertRouteStart contains this logic
            */
            if (curve.FROMMEAS > 528)
            {
                var toInsertB = new VerticalCurve()
                {
                    ROUTE = curve.ROUTE,
                    FROMMEAS = curve.FROMMEAS - 528,
                    TOMEAS = curve.FROMMEAS,
                    DISTANCE = "528",
                    PERCENTGRA = 0,
                    VAFT = ""
                };

                var toIsertA = new VerticalCurve()
                {
                    ROUTE = curve.ROUTE,
                    FROMMEAS = 0,
                    TOMEAS = toInsertB.FROMMEAS,
                    DISTANCE = toInsertB.FROMMEAS.ToString(),
                    PERCENTGRA = 0,
                    VAFT = ""
                };

                InsertVerticalCurve(toIsertA);
                InsertVerticalCurve(toInsertB);

            }
            else
            {

                var toInsert = new VerticalCurve()
                {
                    ROUTE = curve.ROUTE,
                    FROMMEAS = 0,
                    TOMEAS = curve.FROMMEAS,
                    DISTANCE = curve.FROMMEAS.ToString(),
                    PERCENTGRA = 0,
                    VAFT = ""
                };
                InsertVerticalCurve(toInsert);
            }

        }

        public void InsertRouteEnd(VerticalCurve curve, RouteLength route)
        {
            /*
             * if distance is greater than 528 ft, insert two records
             * 1st, the FROMMEAS is the previous TOMEAS, and TOMEAS is FROMMEAS + 528
             * 2nd, FROMMEAS is 1st record's TOMEAS and TOMEAS is ROUTE LENGTH
             */
            if (route.LENGTH - curve.TOMEAS > 528)
            {
                var toInsertA = new VerticalCurve()
                {
                    ROUTE = curve.ROUTE,
                    FROMMEAS = curve.TOMEAS,
                    TOMEAS = curve.TOMEAS + 528,
                    DISTANCE = "528",
                    PERCENTGRA = 0,
                    VAFT = ""
                };

                var toInsertB = new VerticalCurve()
                {
                    ROUTE = curve.ROUTE,
                    FROMMEAS = toInsertA.TOMEAS,
                    TOMEAS = route.LENGTH,
                    DISTANCE = (route.LENGTH - toInsertA.TOMEAS).ToString(),
                    PERCENTGRA = 0,
                    VAFT = ""
                };
                InsertVerticalCurve(toInsertA);
                InsertVerticalCurve(toInsertB);
            }
            else
            {
                var toInsert = new VerticalCurve()
                {
                    ROUTE = curve.ROUTE,
                    FROMMEAS = curve.TOMEAS,
                    TOMEAS = route.LENGTH,
                    DISTANCE = (route.LENGTH - curve.TOMEAS).ToString(),
                    PERCENTGRA = 0,
                    VAFT = ""
                };
                InsertVerticalCurve(toInsert);
            }
        }

        public void InsertRouteGap(VerticalCurve previousCurve, VerticalCurve currentCurve)
        {
            var toInsert = new VerticalCurve()
            {
                ROUTE = currentCurve.ROUTE,
                FROMMEAS = previousCurve.TOMEAS,
                TOMEAS = currentCurve.FROMMEAS,
                DISTANCE = (currentCurve.FROMMEAS - previousCurve.TOMEAS).ToString(),
                PERCENTGRA = 0,
                VAFT = ""
            };
            InsertVerticalCurve(toInsert);

        }

        public void TruncateVerticalCurves()
        {
            var query = "TRUNCATE TABLE VerticalCurves";
            var table = new VerticalCurves().Execute(query);
        }

        public void DeleteVerticalCurve(VerticalCurve curve)
        {
            var sql = "DELETE FROM VerticalCurves WHERE ROUTE = @0 AND FROMMEAS = @1 AND TOMEAS = @2";
            var args = new List<object>();
            args.Add(curve.ROUTE);
            args.Add(curve.FROMMEAS);
            args.Add(curve.TOMEAS);
            var table = new VerticalCurves();
            table.Execute(sql, args: args.ToArray());
            var str = string.Format("DELETED ROUTE: {0}, FROMMEAS: {1}, TOMEAS: {2}", curve.ROUTE, curve.FROMMEAS, curve.TOMEAS);
            Console.WriteLine(str);
        }

    }


    public class RouteLengths : DynamicModel
    {
        public RouteLengths() : base("DbContext", "RouteLengths") { }
    }

    public class VerticalCurvesSource : DynamicModel
    {
        public VerticalCurvesSource() : base("DbContext", "VerticalCurvesSource") { }
    }

    public class VerticalCurves : DynamicModel
    {
        public VerticalCurves() : base("DbContext", "VerticalCurves", primaryKeyField: null) { }
    }
}
