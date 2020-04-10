﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;



namespace PenroseTiling
{
    public class PenroseTilingComponent : GH_Component
    {
        
        public PenroseTilingComponent() : base("PenroseTiling", "Penrose", "Construct Penrose diagram lines", "User", "Test")
        {
        }
        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Number", "num", "Number of level", GH_ParamAccess.item, 2);
            pManager.AddNumberParameter("Length", "length", "Line length", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "lines", "Penrose diagram lines", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Declare start and rule string (ex)
            string finalString = "[7]++[7]++[7]++[7]++[7]";
            string rule6 = "81++91----71[-81----61]++";
            string rule7 = "+81--91[---61--71]+";
            string rule8 = "-61++71[+++81++91]-";
            string rule9 = "--81++++61[+91++++71]--71";

            int num = 0;
            double length = double.NaN;

            if (!DA.GetData(0, ref num)) return;
            if (!DA.GetData(1, ref length)) return;
            
            // We should now validate the data and warn the user if invalid data is supplied.
            if (num <= 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of level must be bigger than One");
                return;
            }
            if (length < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length  must be bigger than Zero");
                return;
            }

            //Generate the string
            GrowString(ref num, ref finalString, rule6, rule7, rule8, rule9);
            //Generate the lines
            var penroseLines = new List<Line>();
            ParsepenroseString(finalString, length, ref penroseLines);

            DA.SetData(0, penroseLines);
        }

        //Generate GrowString from rules
        private void GrowString(ref int num, ref string finalString, string rule6, string rule7, string rule8, string rule9)
        {
            //Decrement the count with each new execution of the grow function
            num = num - 1;
            char rule;

            //Create new string
            string newString = "";
            for (int i = 0; i < finalString.Length; i++)
            {
                rule = finalString[i];
                if (rule == '6')
                {
                    newString = newString + rule6;
                }
                if (rule == '7')
                {
                    newString = newString + rule7;
                }
                if (rule == '8')
                {
                    newString = newString + rule8;
                }
                if (rule == '9')
                {
                    newString = newString + rule9;
                }

                if (rule == '[' || rule == ']' || rule == '+' || rule == '-')
                {
                    newString = newString + rule;
                }
            }
            finalString = newString;

            //Stop condition
            if (num == 0) { return; }

            //Grow agein(recursive)
            GrowString(ref num, ref finalString, rule6, rule7, rule8, rule9);
        }

        //Penrose diagram lines is generate from the penroseString
        private void ParsepenroseString(string penroseString, double length, ref List<Line> penroseLines)
        {
            //Parse instruction string to generate points
            //Let base point be world origin
            var pt = Point3d.Origin;

            //Declare points list
            //Vector rotate with (+.-) instruction by 36degrees
            var listPts = new List<Point3d>();

            //Draw forward direction
            //Vector direction will be rotated depend on (+, -) instructions
            var vec = new Vector3d(1.0, 0.0, 0.0);

            //Stacks of points and vectors
            var ptStack = new List<Point3d>();
            var vStack = new List<Vector3d>();

            //Declare loop variables
            char rule;
            for (int i = 0; i < penroseString.Length; i++)
            {
                //Always start for 1 and length  1 to get one char at a time
                rule = penroseString[i];

                //rotate left
                if (rule == '+')
                {
                    vec.Rotate(36 * (Math.PI / 180), Vector3d.ZAxis);
                }
                //rotate right
                if (rule == '-')
                {
                    vec.Rotate(-36 * (Math.PI / 180), Vector3d.ZAxis);
                }
                //draw forward by direction
                if (rule == '1')
                {
                    //Add current points
                    var newPt1 = new Point3d(pt);
                    listPts.Add(newPt1);

                    //Calculate next point
                    var newPt2 = pt + (vec * length);
                    //Add next point
                    listPts.Add(newPt2);

                    //Save new location
                    pt = newPt2;
                }

                //Save point location
                if (rule == '[')
                {
                    //Save current point and direction
                    var newPt = new Point3d(pt);
                    ptStack.Add(newPt);

                    var newVec = new Vector3d(vec);
                    vStack.Add(newVec);
                }
                //Retrieve point and directon
                if (rule == ']')
                {
                    pt = ptStack[ptStack.Count - 1];
                    vec = vStack[vStack.Count - 1];
                    //Remove from stack
                    ptStack.RemoveAt(ptStack.Count - 1);
                    vStack.RemoveAt(vStack.Count - 1);
                }
            }

            //Generate lines
            var allLines = new List<Line>();
            for (int i = 1; i < listPts.Count; i = i + 2)
            {
                var line = new Line(listPts[i - 1], listPts[i]);
                allLines.Add(line);
            }

            penroseLines = allLines;

        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3becb5e0-906f-4b11-a5f1-6fa658f1992a"); }
        }
    }
}
