using System;
using System.Collections.Generic;
using System.Windows.Forms; // for debugging purpose

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace DDL
{
    public class ReflectComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReflectComponent()
          : base("Reflect", "Reflect",
              "Description",
              "DDL", "UTIL")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("lines", "lines", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("refMesh", "refMesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("ref length", "ref length", "", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.AddPointParameter("hitting pts", "hitting pts", "", GH_ParamAccess.list);
            pManager.AddLineParameter("lines in", "lines in", "", GH_ParamAccess.list);
            pManager.AddLineParameter("lines out", "lines out", "", GH_ParamAccess.list);
            pManager.AddVectorParameter("vecs input", "vecs input", "", GH_ParamAccess.list);
            pManager.AddVectorParameter("vecs ref", "vecs ref", "", GH_ParamAccess.list);
            pManager.AddVectorParameter("face normals", "face normals", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region variables
            double refVecLength = new double();
            List<Point3d> sPts = new List<Point3d>();
            List<Line> lns = new List<Line>();
            Mesh refMesh = new Mesh();

            List<Point3d> pts = new List<Point3d>();
            List<Vector3d> vecsIn = new List<Vector3d>();
            List<Point3d> spts = new List<Point3d>();
            List<int> faceids = new List<int>();
            List<Vector3d> faceNormals = new List<Vector3d>();
            List<Line> lnsIn = new List<Line>();
            List<Line> lnsOut = new List<Line>();
            List<Vector3d> vecsOut = new List<Vector3d>();
            #endregion

            #region input
            if (!DA.GetDataList(0, lns)) { return; }
            if (!DA.GetData(1, ref refMesh)) { return; }
            if (!DA.GetData(2, ref refVecLength)) { return; }
            #endregion

            #region solve
            refMesh.FaceNormals.ComputeFaceNormals();
            for (int i = 0; i < lns.Count; i++)
            {

                Point3d spt = lns[i].From;
                Point3d ept = lns[i].To;
                Vector3d rayvec = new Vector3d(ept.X-spt.X, ept.Y-spt.Y, ept.Z-spt.Z);
                Ray3d vecray = new Ray3d(spt, rayvec);

                double hitPtRayParam = Rhino.Geometry.Intersect.Intersection.MeshRay(refMesh, vecray, out int[] tmpFaceIds);
                
                if (hitPtRayParam > 0.001) // 
                // if (hitPtRayParam >= 0)
                {
                    Point3d hitPt = lns[i].PointAt(hitPtRayParam);
                    pts.Add(hitPt); 
                    vecsIn.Add(Normalize(new Vector3d(
                            (lns[i].ToX - lns[i].FromX), 
                            (lns[i].ToY - lns[i].FromY), 
                            (lns[i].ToZ - lns[i].FromZ))
                        )); 
                    sPts.Add(lns[i].From);
                    faceids.Add(tmpFaceIds[0]);
                    faceNormals.Add(refMesh.FaceNormals[tmpFaceIds[0]]);
                    lnsIn.Add(new Line(lns[i].From, hitPt));
                }

            }
            
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3d nml = (-1) * faceNormals[i];
                Vector3d ivec = vecsIn[i];
                double factorA = nml.X * ivec.X + nml.Y * ivec.Y + nml.Z * ivec.Z;
                Vector3d refVec = ivec - 2 * factorA * nml;
                vecsOut.Add(refVec);
                lnsOut.Add(new Line(pts[i], refVec, refVecLength));
            }
            #endregion

            #region output
            DA.SetDataList(0, pts);
            DA.SetDataList(1, lnsIn);
            DA.SetDataList(2, lnsOut);
            DA.SetDataList(3, vecsIn);
            DA.SetDataList(4, vecsOut);
            DA.SetDataList(5, faceNormals);
            #endregion

        }

        protected Vector3d Normalize(Vector3d _vec)
        {
            double length = Math.Sqrt(Math.Pow(_vec.X,2) + Math.Pow(_vec.Y,2) + Math.Pow(_vec.Z,2));
            _vec = _vec / length;

            return _vec;
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
                return Reflect.Properties.Resources.icon_reflect; //  DDL.Properties.;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6e0a5ae8-4c78-4211-8ec3-d26302152e71"); }
        }
    }
}
