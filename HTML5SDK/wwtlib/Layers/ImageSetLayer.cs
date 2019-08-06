﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Html;
using System.Xml;
using System.Html.Services;
using System.Html.Media.Graphics;

namespace wwtlib
{
    public class ImageSetLayer : Layer
    {
        Imageset imageSet = null;

        ScaleTypes lastScale = ScaleTypes.Linear;
        double min = 0;
        double max = 0;

        public Imageset ImageSet
        {
            get { return imageSet; }
            set { imageSet = value; }
        }
        string extension = ".txt";
        public ImageSetLayer()
        {

        }

        public static ImageSetLayer Create(Imageset set)
        {
            ImageSetLayer isl = new ImageSetLayer();
            isl.imageSet = set;
            return isl;
        }

        bool overrideDefaultLayer = false;
        public bool OverrideDefaultLayer
        {
            get { return overrideDefaultLayer; }
            set { overrideDefaultLayer = value; }
        }

        public FitsImage GetFitsImage()
        {
            return imageSet.WcsImage as FitsImage;
        }

        public override void InitializeFromXml(XmlNode node)
        {
            XmlNode imageSetNode = Util.SelectSingleNode(node, "ImageSet");

            imageSet = Imageset.FromXMLNode(imageSetNode);


            if (node.Attributes.GetNamedItem("Extension") != null)
            {
                extension = node.Attributes.GetNamedItem("Extension").Value;
            }

            if (node.Attributes.GetNamedItem("ScaleType") != null)
            {
                lastScale = (ScaleTypes)Enums.Parse("ScaleTypes", node.Attributes.GetNamedItem("ScaleType").Value);
            }

            if (node.Attributes.GetNamedItem("MinValue") != null)
            {
                min = double.Parse(node.Attributes.GetNamedItem("MinValue").Value);
            }

            if (node.Attributes.GetNamedItem("MaxValue") != null)
            {
                max = double.Parse(node.Attributes.GetNamedItem("MaxValue").Value);
            }

            if (node.Attributes.GetNamedItem("OverrideDefault") != null)
            {
                overrideDefaultLayer = bool.Parse(node.Attributes.GetNamedItem("OverrideDefault").Value);
            }

        }

        bool loaded = false;
        public override bool Draw(RenderContext renderContext, float opacity, bool flat)
        {
            if (!loaded)
            {
                return false;
            }
            //if (!flat)
            //{
            //    renderContext.setRasterizerState(TriangleCullMode.CullClockwise);
            //}
            renderContext.WorldBase = renderContext.World;
            renderContext.ViewBase = renderContext.View;
            renderContext.MakeFrustum();
            renderContext.DrawImageSet(imageSet, this.Opacity * opacity * 100);
            return true;

        }

        public override void WriteLayerProperties(XmlTextWriter xmlWriter)
        {
            if (imageSet.WcsImage != null)
            {
                if (imageSet.WcsImage is FitsImage)
                {
                    extension = ".fit";
                }
                else
                {
                    extension = ".png";
                }
                xmlWriter.WriteAttributeString("Extension", extension);
            }

            if (imageSet.WcsImage is FitsImage)
            {
                FitsImage fi = imageSet.WcsImage as FitsImage;
                xmlWriter.WriteAttributeString("ScaleType", fi.lastScale.ToString());
                xmlWriter.WriteAttributeString("MinValue", fi.lastBitmapMin.ToString());
                xmlWriter.WriteAttributeString("MaxValue", fi.lastBitmapMax.ToString());

            }

            xmlWriter.WriteAttributeString("OverrideDefault", overrideDefaultLayer.ToString());

            Imageset.SaveToXml(xmlWriter, imageSet, "");
            base.WriteLayerProperties(xmlWriter);
        }

        public override string GetTypeName()
        {
            return "TerraViewer.ImageSetLayer";
        }

        public override void CleanUp()
        {
            base.CleanUp();
        }

        public override void AddFilesToCabinet(FileCabinet fc)
        {
            if (imageSet.WcsImage is FitsImage)
            {
                string fName = ((WcsImage)imageSet.WcsImage).Filename;

                string fileName = fc.TempDirectory + string.Format("{0}\\{1}{2}", fc.PackageID, this.ID.ToString(), extension);

                fc.AddFile(fileName, ((FitsImage)imageSet.WcsImage).sourceBlob);
            }
        }

        public override string[] GetParamNames()
        {
            return base.GetParamNames();
        }

        public override double[] GetParams()
        {
            return base.GetParams();
        }

        public override void SetParams(double[] paramList)
        {
            base.SetParams(paramList);
        }

        public void SetImageScale(ScaleTypes scaleType, double min, double max)
        {
            Script.Literal("console.warn('SetImageScale is considered deprecated. Use setImageScaleRaw or setImageScalePhysical instead.')");
            SetImageScaleRaw(scaleType, min, max);
        }

        public void SetImageScaleRaw(ScaleTypes scaleType, double min, double max)
        {
            this.min = min;
            this.max = max;
            this.lastScale = scaleType;

            if (imageSet.WcsImage is FitsImage)
            {
                Histogram.UpdateScale(this, scaleType, min, max);
            }
        }

        public void SetImageScalePhysical(ScaleTypes scaleType, double min, double max)
        {
            double newMin = min;
            double newMax = max;
            if (imageSet.WcsImage is FitsImage)
            {
                FitsImage img = imageSet.WcsImage as FitsImage;
                newMin = (newMin - img.BZero) / img.BScale;
                newMax = (newMax - img.BZero) / img.BScale;
            }
            SetImageScaleRaw(scaleType, newMin, newMax);
        }

        public void SetImageZ(double z)
        {
            if (imageSet.WcsImage is FitsImage)
            {
                Histogram.UpdateImage(this, z);
            }
        }

        public override void LoadData(TourDocument tourDoc, string filename)
        {
            if (extension.ToLowerCase().StartsWith(".fit"))
            {
                System.Html.Data.Files.Blob blob = tourDoc.GetFileBlob(filename.Replace(".txt", extension));

                FitsImage fi = new FitsImage("image.fit", blob, DoneLoading);
                imageSet.WcsImage = fi;
                if (max > 0 || min > 0)
                {
                    fi.lastBitmapMax = max;
                    fi.lastBitmapMin = min;
                    fi.lastScale = lastScale;
                }
            }
            else
            {
                loaded = true;
            }

        }

        public void DoneLoading(WcsImage wcsImage)
        {
             loaded = true;
        }
    }
}
