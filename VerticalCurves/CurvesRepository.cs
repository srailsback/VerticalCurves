using Massive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerticalCurves
{
    public interface ICurvesRepository
    {
        List<RouteLength> GetAllRouteLengths();

        List<VerticalCurve> GetAllVerticalCurves(bool isSource);

        List<VerticalCurve> GetVerticalCurvesByRoute(string route, bool isSource);

        void InsertVerticalCurve(VerticalCurve curve);

        void UpdateVerticalCurve(VerticalCurve curve);

        void InsertRouteStart(VerticalCurve curve);

        void InsertRouteEnd(VerticalCurve curve, RouteLength route);

        void InsertRouteGap(VerticalCurve previousCurve, VerticalCurve currentCurve);

        void TruncateVerticalCurves();

    }

    public class CurvesRepository : ICurvesRepository
    {
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
            var str = string.Format("INSERTED ROUTE: {0}, FROMMEAS: {1}, TOMEAS: {2}", curve.ROUTE, curve.FROMMEAS, curve.TOMEAS);
            Console.WriteLine(str);

        }

        public void UpdateVerticalCurve(VerticalCurve curve)
        {
            var query = "UPDATE VerticalCurves SET VAFT = @0 WHERE ROUTE = @1 AND FROMMEAS = @2 AND TOMEAS = @3";
            var args = new List<object>();
            args.Add(curve.VAFT);
            args.Add(curve.ROUTE);
            args.Add(curve.FROMMEAS);
            args.Add(curve.TOMEAS);
            var table = new VerticalCurves();
            table.Execute(query, args: args.ToArray());
            var str = string.Format("UPDATED ROUTE: {0}, FROMMEAS: {1}, TOMEAS: {2}, VAFT: {3}", curve.ROUTE, curve.FROMMEAS, curve.TOMEAS, curve.VAFT);
            Console.WriteLine(str);
        }

        public void InsertRouteStart(VerticalCurve curve)
        {
            var toInsert = new VerticalCurve()
            {
                ROUTE = curve.ROUTE,
                FROMMEAS = 0,
                TOMEAS = curve.FROMMEAS,
                DISTANCE = curve.FROMMEAS,
                PERCENTGRA = 0,
                VAFT = ""
            };
            InsertVerticalCurve(toInsert);
        }
        
        public void InsertRouteEnd(VerticalCurve curve, RouteLength route)
        {
            var toInsert = new VerticalCurve()
            {
                ROUTE = curve.ROUTE,
                FROMMEAS = curve.TOMEAS,
                TOMEAS = route.LENGTH,
                DISTANCE = route.LENGTH - curve.TOMEAS,
                PERCENTGRA = 0,
                VAFT = ""
            };
            InsertVerticalCurve(toInsert);
        }

        public void InsertRouteGap(VerticalCurve previousCurve, VerticalCurve currentCurve)
        {
            var toInsert = new VerticalCurve()
            {
                ROUTE = currentCurve.ROUTE,
                FROMMEAS = previousCurve.TOMEAS,
                TOMEAS = currentCurve.FROMMEAS,
                DISTANCE = currentCurve.FROMMEAS - previousCurve.TOMEAS,
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
