#region Copyright
///<remarks>
/// <GRAL Graphical User Interface GUI>
/// Copyright (C) [2019]  [Dietmar Oettl, Markus Kuntner]
/// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
/// the Free Software Foundation version 3 of the License
/// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
/// You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
///</remarks>
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using GralDomForms;
using GralIO;
using GralItemData;
using GralStaticFunctions;

namespace GralDomain
{
    public partial class Domain
    {
        /// <summary>
        /// Left mousekey down events on the picturebox
        /// </summary>
        private void Picturebox1MouseDownLeft(object sender, MouseEventArgs e)
        {
            bool shift_key_pressed = false; // needed for endpoints of GRAMM and GRAL domain when using manual input coordinates
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) // Kuntner: manual coordinate-Input
            {
                using (InputCoordinates inp = new InputCoordinates(textBox1.Text, textBox2.Text))
                {
                    inp.TopMost = true;
                    inp.ShowDialog();
                    textBox1.Text = inp.Input_x.Text;
                    textBox2.Text = inp.Input_y.Text;
                    toolStripTextBox1.Text = textBox1.Text;
                    toolStripTextBox2.Text = textBox2.Text;
                    shift_key_pressed = true;
                }

                //textBox1.Text = Convert.ToString(Math.Round((e.X-transformx) * bmppbx * PixelXSize + WesternBorderX0, 1, MidpointRounding.AwayFromZero));
                //textBox2.Text = Convert.ToString(Math.Round((e.Y-transformy) * bmppbx * PixelYSize + NorthernBorderY0, 1, MidpointRounding.AwayFromZero));
            }

#if __MonoCS__
#else
            //ToolTip for lenght measurement
            if ((MouseControl == MouseMode.LineSourcePos || MouseControl == MouseMode.BuildingPos || MouseControl == MouseMode.AreaSourcePos || MouseControl == MouseMode.ViewDistanceMeasurement || MouseControl == MouseMode.ViewAreaMeasurement || MouseControl == MouseMode.WallSet || MouseControl == MouseMode.VegetationPosCorner) && ShowLenghtLabel)
            {
                ToolTipMousePosition.Active = true; // show tool tip lenght of rubberline segment
                FirstPointLenght.X = (float)St_F.TxtToDbl(textBox1.Text, false);
                FirstPointLenght.Y = (float)St_F.TxtToDbl(textBox2.Text, false);
            }
#endif
            // send Message to all forms registered to the SendClickedCoordinates event
            try
            {
                if (SendCoors != null)
                {
                    PointD _pt = new PointD(Convert.ToDouble(textBox1.Text), Convert.ToDouble(textBox2.Text));
                    SendCoors(_pt, e);
                }
            }
            catch
            { }

            switch (MouseControl)
            {
                case MouseMode.ZoomIn:
                    //Zoom in
                    ZoomPlusMinus(1, e);
                    break;

                case MouseMode.ZoomOut:
                    //Zoom out
                    ZoomPlusMinus(-1, e);
                    break;

                case MouseMode.ViewPanelZoom:
                    //Panel zoom
                    XDomain = e.X;
                    YDomain = e.Y;
                    MouseControl = MouseMode.ViewPanelZoomArea;
                    break;

                case MouseMode.ViewMoveMap:
                    //Move map
                    OldXPosition = e.X;
                    OldYPosition = e.Y;
                    break;

                case MouseMode.BaseMapGeoReference1:
                    //Georeferencing1
                    //Convert PictureBox-Coordinates in Picture-Coordinates
                    GeoReferenceOne.XMouse = (Convert.ToDouble(e.X) - (TransformX + Convert.ToInt32((ItemOptions[0].West - MapSize.West) / BmpScale / MapSize.SizeX))) / ItemOptions[0].PixelMx * MapSize.SizeX / XFac;
                    GeoReferenceOne.YMouse = (Convert.ToDouble(e.Y) - (TransformY - Convert.ToInt32((ItemOptions[0].North - MapSize.North) / BmpScale / MapSize.SizeX))) / ItemOptions[0].PixelMx * MapSize.SizeX / XFac;
                    GeoReferenceOne.Refresh();
                    break;

                case MouseMode.BaseMapGeoReference2:
                    //Georeferencing2
                    //Convert PictureBox-Coordinates in Picture-Coordinates
                    GeoReferenceTwo.XMouse = (Convert.ToDouble(e.X) - (TransformX + Convert.ToInt32((ItemOptions[0].West - MapSize.West) / BmpScale / MapSize.SizeX))) / ItemOptions[0].PixelMx * MapSize.SizeX / XFac;
                    GeoReferenceTwo.YMouse = (Convert.ToDouble(e.Y) - (TransformY - Convert.ToInt32((ItemOptions[0].North - MapSize.North) / BmpScale / MapSize.SizeX))) / ItemOptions[0].PixelMx * MapSize.SizeX / XFac;
                    GeoReferenceTwo.Refresh();
                    break;

                case MouseMode.GralDomainEndPoint:
                    // set endpoint of GRAL-Domain when using shift-Key
                    // calculate the GRAL-Domain
                    {
                        int xm = Convert.ToInt32((Convert.ToDouble(textBox1.Text.Replace(".", decsep)) - MapSize.West) / (BmpScale * MapSize.SizeX) + TransformX);
                        int ym = Convert.ToInt32((Convert.ToDouble(textBox2.Text.Replace(".", decsep)) - MapSize.North) / (BmpScale * MapSize.SizeY) + TransformY);

                        int x1 = Math.Min(xm, XDomain);
                        int y1 = Math.Min(ym, YDomain);
                        int x2 = Math.Max(xm, XDomain);
                        int y2 = Math.Max(ym, YDomain);
                        int recwidth = x2 - x1;
                        int recheigth = y2 - y1;
                        GRALDomain = new Rectangle(x1, y1, recwidth, recheigth);
                        XDomain = 0;
                        Picturebox1_MouseUp(null, e); // force button up event
                    }
                    break;

                case MouseMode.GralDomainStartPoint:
                    //get starting point for drawing GRAL model domain
                    if (Gral.Main.Project_Locked == false)
                    {
                        XDomain = Convert.ToInt32((Convert.ToDouble(textBox1.Text.Replace(".", decsep)) - MapSize.West) / (BmpScale * MapSize.SizeX) + TransformX);
                        YDomain = Convert.ToInt32((Convert.ToDouble(textBox2.Text.Replace(".", decsep)) - MapSize.North) / (BmpScale * MapSize.SizeY) + TransformY);
                        Cursor.Clip = Bounds;
                        MouseControl = MouseMode.GralDomainEndPoint;
                    }
                    break;

                case MouseMode.PointSourcePos:
                case MouseMode.PointSourceInlineEdit:
                    //digitize position of point source
                    if (Gral.Main.Project_Locked == false)
                    {
                        //get x,y coordinates
                        EditPS.SetXCoorText(textBox1.Text);
                        EditPS.SetYCoorText(textBox2.Text);
                        EditPS.SaveArray(false);
                        if (MouseControl == MouseMode.PointSourceInlineEdit) // set new position inline editing
                        {
                            EditAndSavePointSourceData(sender, null);
                            InfoBoxCloseAllForms();
                            MouseControl = MouseMode.PointSourceDeQueue;
                        }

                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.AreaSourcePos:
                    //digitize position of the corner points of area sources
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control && EditAS.CornerAreaX.Length > 1) // Kuntner: change one edge-point of area source
                    {
                        // Change one edge of the area source
                        MoveEdgepointArea();
                    }
                    else
                    {
                        if (EditSourceShape == false && Gral.Main.Project_Locked == false)
                        {
                            if (EditAS.ItemDisplayNr < EditAS.ItemData.Count && EditAS.ItemData[EditAS.ItemDisplayNr].Pt.Count > 0)
                            {
                                if (MessageBox.Show(this, "Input new and delete current shape?", "Edit vertices", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    EditSourceShape = true;
                                }
                            }
                            else // new item
                            {
                                EditSourceShape = true;
                            }
                        }
                        if (EditSourceShape)
                        {
                            double x = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                            double y = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                            //Skip double click on same coor
                            if (EditAS.CornerAreaCount > 1 && Math.Abs(x - EditAS.CornerAreaX[EditAS.CornerAreaCount - 1]) < 0.01 &&
                                                              Math.Abs(y - EditAS.CornerAreaY[EditAS.CornerAreaCount - 1]) < 0.01)
                            { }
                            else
                            {
                                //set new area source - get x,y coordinates
                                CornerAreaSource[EditAS.CornerAreaCount] = new Point(e.X, e.Y);
                                EditAS.CornerAreaX[EditAS.CornerAreaCount] = x;
                                EditAS.CornerAreaY[EditAS.CornerAreaCount] = y;
                                EditAS.CornerAreaCount += 1;
                                EditAS.SetNumberOfVerticesText(Convert.ToString(EditAS.CornerAreaCount));
                            }
                            // Reset Rubber-Line Drawing
                            Cursor.Clip = Bounds;
                            RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                            Picturebox1_Paint(); // 
                        }
                        else
                        {
                            ToolTipMousePosition.Active = false;
                        }
                    }
                    break;

                case MouseMode.BuildingPos:
                    //digitize position of the corner points of buildings
                    if (Gral.Main.Project_Locked == false)
                    {
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control && EditB.CornerBuildingX.Length > 1) // Kuntner: change edge point of a building
                        {
                            // Change one edge of the building
                            MoveEdgepointBuilding();
                        }
                        else
                        {
                            if (EditSourceShape == false && Gral.Main.Project_Locked == false)
                            {
                                if (EditB.ItemDisplayNr < EditB.ItemData.Count && EditB.ItemData[EditB.ItemDisplayNr].Pt.Count > 0)
                                {
                                    if (MessageBox.Show(this, "Input new and delete current shape?", "Edit vertices", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                    {
                                        EditSourceShape = true;
                                    }
                                }
                                else // new item
                                {
                                    EditSourceShape = true;
                                }
                            }

                            if (EditSourceShape)
                            {
                                double x = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                                double y = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                                //Skip double click on same coor
                                if (EditB.CornerBuilding > 1 && Math.Abs(x - EditB.CornerBuildingX[EditB.CornerBuilding - 1]) < 0.01 &&
                                                                Math.Abs(y - EditB.CornerBuildingY[EditB.CornerBuilding - 1]) < 0.01)
                                { }
                                else
                                {
                                    //set new building - get x,y coordinates
                                    CornerAreaSource[EditB.CornerBuilding] = new Point(e.X, e.Y);
                                    EditB.CornerBuildingX[EditB.CornerBuilding] = x;
                                    EditB.CornerBuildingY[EditB.CornerBuilding] = y;
                                    EditB.CornerBuilding++;
                                    EditB.SetNumberOfVerticesText(Convert.ToString(EditB.CornerBuilding));
                                }
                                // Reset Rubber-Line Drawing
                                Cursor.Clip = Bounds;
                                RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                                Picturebox1_Paint(); // 
                            }
                            else
                            {
                                ToolTipMousePosition.Active = false;
                            }
                        }
                    }
                    break;

                case MouseMode.VegetationPosCorner:
                    //digitize position of the corner points of forests
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control && EditAS.CornerAreaX.Length > 1) // Kuntner: change one edge-point of area source
                    {
                        // Change one edge of the area source
                        MoveEdgepointVegetation();
                    }
                    else
                    {
                        if (EditSourceShape == false && Gral.Main.Project_Locked == false)
                        {
                            if (EditVegetation.ItemDisplayNr < EditVegetation.ItemData.Count && EditVegetation.ItemData[EditVegetation.ItemDisplayNr].Pt.Count > 0)
                            {
                                if (MessageBox.Show(this, "Input new and delete current shape?", "Edit vertices", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    EditSourceShape = true;
                                }
                            }
                            else // new item
                            {
                                EditSourceShape = true;
                            }
                        }
                        if (EditSourceShape)
                        {
                            double x = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                            double y = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                            //Skip double click on same coor
                            if (EditVegetation.CornerVegetation > 1 && Math.Abs(x - EditVegetation.CornerVegX[EditVegetation.CornerVegetation - 1]) < 0.01 &&
                                                                       Math.Abs(y - EditVegetation.CornerVegY[EditVegetation.CornerVegetation - 1]) < 0.01)
                            { }
                            else
                            {
                                //set new area source - get x,y coordinates
                                CornerAreaSource[EditVegetation.CornerVegetation] = new Point(e.X, e.Y);
                                EditVegetation.CornerVegX[EditVegetation.CornerVegetation] = x;
                                EditVegetation.CornerVegY[EditVegetation.CornerVegetation] = y;
                                EditVegetation.CornerVegetation += 1;
                                EditVegetation.SetNumberOfVerticesText(Convert.ToString(EditVegetation.CornerVegetation));
                            }
                            // Reset Rubber-Line Drawing
                            Cursor.Clip = Bounds;
                            RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                            Picturebox1_Paint(); // 
                        }
                        else
                        {
                            ToolTipMousePosition.Active = false;
                        }
                    }
                    break;

                case MouseMode.LineSourcePos:
                    //digitize position of the corner points of line sources
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control && EditLS.CornerLineX.Length > 1) // Kuntner: change point of line source
                    {
                        // Change one edge of the line source
                        MoveEdgepointLine();
                    }
                    else
                    {
                        if (EditSourceShape == false && Gral.Main.Project_Locked == false)
                        {
                            if (EditLS.ItemDisplayNr < EditLS.ItemData.Count && EditLS.ItemData[EditLS.ItemDisplayNr].Pt.Count > 0)
                            {
                                if (MessageBox.Show(this, "Input new and delete current shape?", "Edit vertices", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    EditSourceShape = true;
                                }
                            }
                            else // new item
                            {
                                EditSourceShape = true;
                            }
                        }
                        if (EditSourceShape)
                        {
                            double x = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                            double y = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                            //Skip double click on same coor
                            if (EditLS.CornerLineSource > 1 && Math.Abs(x - EditLS.CornerLineX[EditLS.CornerLineSource - 1]) < 0.01 &&
                                                               Math.Abs(y - EditLS.CornerLineY[EditLS.CornerLineSource - 1]) < 0.01)
                            {}
                            else
                            { 
                                // set new line-source edge point - get x,y coordinates
                                CornerAreaSource[EditLS.CornerLineSource] = new Point(e.X, e.Y);
                                EditLS.CornerLineX[EditLS.CornerLineSource] = x;
                                EditLS.CornerLineY[EditLS.CornerLineSource] = y;
                                EditLS.CornerLineSource += 1;
                                EditLS.SetNumberOfVerticesText(Convert.ToString(EditLS.CornerLineSource));
                            }
                            // Reset Rubber-Line Drawing
                            Cursor.Clip = Bounds;
                            RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                            Picturebox1_Paint(); // 

                        }
                        else
                        {
                            ToolTipMousePosition.Active = false;
                        }
                    }
                    break;

                case MouseMode.WallSet:
                    //digitize position of the corner points of walls
                    if (Gral.Main.Project_Locked == false)
                    {
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control && EditLS.CornerLineX.Length > 1) // Kuntner: change point of wall
                        {
                            // Change one edge of the line source
                            MoveEdgepointWall();
                        }
                        else
                        {
                            if (EditSourceShape == false && Gral.Main.Project_Locked == false)
                            {
                                if (EditWall.ItemDisplayNr < EditWall.ItemData.Count && EditWall.ItemData[EditWall.ItemDisplayNr].Pt.Count > 0)
                                {
                                    if (MessageBox.Show(this, "Input new and delete current shape?", "Edit vertices", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                    {
                                        EditSourceShape = true;
                                    }
                                }
                                else // new item
                                {
                                    EditSourceShape = true;
                                }
                            }
                            if (EditSourceShape)
                            {
                                double x = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                                double y = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                                //Skip double click on same coor
                                if (EditWall.CornerWallCount > 1 && Math.Abs(x - EditWall.CornerWallX[EditWall.CornerWallCount - 1]) < 0.01 &&
                                                                    Math.Abs(y - EditWall.CornerWallY[EditWall.CornerWallCount - 1]) < 0.01)
                                { }
                                else
                                {
                                    // set new wall - get x,y coordinates
                                    CornerAreaSource[EditWall.CornerWallCount] = new Point(e.X, e.Y);
                                    EditWall.CornerWallX[EditWall.CornerWallCount] = x;
                                    EditWall.CornerWallY[EditWall.CornerWallCount] = y;
                                    EditWall.CornerWallZ[EditWall.CornerWallCount] = EditWall.GetNumericUpDownHeightValue();
                                    if (EditWall.CheckboxAbsHeightChecked()) // absolute height
                                    {
                                        EditWall.CornerWallZ[EditWall.CornerWallCount] *= -1;
                                    }

                                    EditWall.CornerWallCount += 1;
                                    EditWall.SetNumberOfVerticesText(Convert.ToString(EditWall.CornerWallCount));
                                }
                                // Reset Rubber-Line Drawing
                                Cursor.Clip = Bounds;
                                RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                                Picturebox1_Paint(); // 
                            }
                            else
                            {
                                ToolTipMousePosition.Active = false;
                            }
                        }
                    }
                    break;

                case MouseMode.ReceptorPos:
                case MouseMode.ReceptorInlineEdit:
                    //digitize position of receptors
                    if (Gral.Main.Project_Locked == false)
                    {
                        //get x,y coordinates
                        EditR.SetXCoorText(textBox1.Text);
                        EditR.SetYCoorText(textBox2.Text);
                        EditR.SaveArray(false);
                        if (MouseControl == MouseMode.ReceptorInlineEdit) // set new position inline editing
                        {
                            EditAndSaveReceptorData(sender, null);

                            InfoBoxCloseAllForms();
                            MouseControl = MouseMode.ReceptorDeQueue;
                        }

                        Picturebox1_Paint(); // 
                    }
                    break;

                    // Tooltip for picturebox1

                case MouseMode.PointSourceSel:
                    //select point sources
                    {
                        int i = 0;
                        int found = -1;
                        PointSourceData _foundobj = null;
                        
                        foreach (PointSourceData _psdata in EditPS.ItemData)
                        {
                            int x1 = (int)((_psdata.Pt.X - MapSize.West) / BmpScale / MapSize.SizeX) + TransformX;
                            int y1 = (int)((_psdata.Pt.Y - MapSize.North) / BmpScale / MapSize.SizeY) + TransformY;

                            if ((e.X >= x1 - 10) && (e.X <= x1 + 10) && (e.Y >= y1 - 10) && (e.Y <= y1 + 10))
                            {
                                found = i;
                                _foundobj = _psdata;
                                break;
                            }
                            i += 1;
                        }
                        if (found > -1 && _foundobj != null)
                        {
                            EditPS.SetTrackBar(found + 1);
                            EditPS.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditPS.FillValues();

                            double height = _foundobj.Height;

                            // show info in a Tooltip
                            string infotext = "'" + _foundobj.Name + "'\n";
                            if (height >= 0)
                            {
                                infotext += "Height (rel) [m]: " + Math.Round(height, 1).ToString() + "\n";
                            }
                            else
                            {
                                infotext += "Height (abs) [m]: " + Math.Abs(Math.Round(height, 1)).ToString() + "\n";
                            }

                            infotext += "Exit velocity [m/s]:  " + Math.Round(_foundobj.Velocity, 1).ToString() + "\n";
                            infotext += "Exit temperature [K]: " + Math.Round(_foundobj.Temperature, 1).ToString() + "\n";
                            infotext += "Diameter [m]: " + Math.Round(_foundobj.Diameter, 2).ToString() + "\n";
                            infotext += "Source group: " + Convert.ToString(_foundobj.Poll.SourceGroup) + "\n";
                            
                            double[] emission = new double[Gral.Main.PollutantList.Count];
                            for (int r = 0; r < 10; r++)
                            {
                                try
                                {
                                    int index = _foundobj.Poll.Pollutant[r];
                                    emission[index] = emission[index] + Convert.ToDouble(_foundobj.Poll.EmissionRate[r]);
                                }
                                catch { }
                            }
                            for (int r = 0; r < Gral.Main.PollutantList.Count; r++)
                            {
                                if (emission[r] > 0)
                                {
                                    if (Gral.Main.PollutantList[r] != "Odour")
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[kg/h]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                    else
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[MOU/h]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                }
                            }
                            AddItemInfoToDrawingObject(infotext, _foundobj.Pt.X, _foundobj.Pt.Y);
                        }
                        Focus();
                    }
                    break;

                case MouseMode.PointSourceDeQueue:
                    // delete one mouseclick from queue
                    MouseControl = MouseMode.PointSourceSel;
                    break;

                case MouseMode.ReceptorSel:
                    //select receptors
                    {
                        int i = 0;
                        int found = -1;
                        ReceptorData _foundobj = null;
                        foreach (ReceptorData _rd in EditR.ItemData)
                        {
                            int x1 = Convert.ToInt32((_rd.Pt.X - MapSize.West) / BmpScale / MapSize.SizeX) + TransformX;
                            int y1 = Convert.ToInt32((_rd.Pt.Y - MapSize.North) / BmpScale / MapSize.SizeY) + TransformY;
                            if ((e.X >= x1 - 10) && (e.X <= x1 + 10) && (e.Y >= y1 - 10) && (e.Y <= y1 + 10))
                            {
                                found = i;
                                _foundobj = _rd;
                                break;
                            }
                            i++;
                        }
                        if (found > -1 && _foundobj != null)
                        {
                            EditR.SetTrackBar(found + 1);
                            EditR.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditR.FillValues();
                            //Ausgabe der Info in Infobox
                            string infotext = "'" + _foundobj.Name + "'\n";
                            infotext += "Height [m]: " + Math.Round(_foundobj.Height, 1).ToString();
                            AddItemInfoToDrawingObject(infotext, _foundobj.Pt.X, _foundobj.Pt.Y);
                        }
                        Focus();
                    }
                    break;

                case MouseMode.ReceptorDeQueue:
                    // delete one mouseclick from queue
                    MouseControl = MouseMode.ReceptorSel;
                    break;

                case MouseMode.AreaSourceSel:
                    //select area sources
                    {
                        int i = 0;
                        int found = -1;
                        AreaSourceData _foundobj = null;
                        List<Point> poly = new List<Point>();

                        foreach (AreaSourceData _as in EditAS.ItemData)
                        {
                            poly.Clear();

                            List<PointD> _points = _as.Pt;
                            int x1 = 0;
                            int y1 = 0;
                            for (int j = 0; j < _points.Count; j++)
                            {
                                x1 = Convert.ToInt32((_points[j].X - MapSize.West) / BmpScale / MapSize.SizeX) + TransformX;
                                y1 = Convert.ToInt32((_points[j].Y - MapSize.North) / BmpScale / MapSize.SizeY) + TransformY;
                                poly.Add(new Point(x1, y1));
                            }

                            if (St_F.PointInPolygon(new Point(e.X, e.Y), poly))
                            {
                                found = i;
                                _foundobj = _as;
                                break;
                            }
                            i += 1;
                        }

                        if (found > -1 && _foundobj != null)
                        {
                            EditAS.SetTrackBar(found + 1);
                            EditAS.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditAS.FillValues();

                            double height = _foundobj.Height;

                            //Ausgabe der Info in Infobox

                            string infotext = "'" + _foundobj.Name + "'\n";
                            if (height >= 0)
                            {
                                infotext += "Mean height (rel) [m]:  " + Math.Round(height, 1).ToString() + "\n";
                            }
                            else
                            {
                                infotext += "Mean height (abs) [m]:  " + Math.Abs(Math.Round(height, 1)).ToString() + "\n";
                            }

                            infotext += "Vertical extension [m]: " + Math.Round(_foundobj.VerticalExt, 1).ToString() + "\n";
                            infotext += @"Area [m" + Gral.Main.SquareString + "]: " + Math.Round(_foundobj.Area, 1).ToString() + "\n";
                            infotext += "Source group: " + _foundobj.Poll.SourceGroup + "\n";
                            double[] emission = new double[Gral.Main.PollutantList.Count];
                            for (int r = 0; r < 10; r++)
                            {
                                try
                                {
                                    int index = _foundobj.Poll.Pollutant[r];
                                    emission[index] = emission[index] + _foundobj.Poll.EmissionRate[r];
                                }
                                catch { }
                            }
                            for (int r = 0; r < Gral.Main.PollutantList.Count; r++)
                            {
                                if (emission[r] > 0)
                                {
                                    if (Gral.Main.PollutantList[r] != "Odour")
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[kg/h]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                    else
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[MOU/h]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                }
                            }

                            AddItemInfoToDrawingObject(infotext, (float)St_F.TxtToDbl(textBox1.Text, false), (float)St_F.TxtToDbl(textBox2.Text, false));
                        }
                        Focus();
                    }
                    break;

                case MouseMode.VegetationSel:
                    //select vegetation
                    {
                        int i = 0;
                        int found = -1;
                        VegetationData _foundobj = null;
                        int x1 = 0;
                        int y1 = 0;
                        List<Point> poly = new List<Point>();

                        foreach (VegetationData _vdata in EditVegetation.ItemData)
                        {
                            poly.Clear();
                            List<PointD> _points = _vdata.Pt;
                            for (int j = 0; j < _points.Count; j++)
                            {
                                x1 = Convert.ToInt32((_points[j].X - MapSize.West) / BmpScale / MapSize.SizeX) + TransformX;
                                y1 = Convert.ToInt32((_points[j].Y - MapSize.North) / BmpScale / MapSize.SizeY) + TransformY;
                                poly.Add(new Point(x1, y1));
                            }

                            if (St_F.PointInPolygon(new Point(e.X, e.Y), poly))
                            {
                                found = i;
                                _foundobj = _vdata;
                                break;
                            }
                            i += 1;
                        }
                        if (found > -1 && _foundobj != null)
                        {
                            EditVegetation.SetTrackBar(found + 1);
                            EditVegetation.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditVegetation.FillValues();
                            double height = _foundobj.VerticalExt;

                            //Ausgabe der Info in Infobox
                            string infotext = "'" + _foundobj.Name + "'\n";
                            infotext += "Height (rel) [m]: " + Math.Abs(Math.Round(height, 1)) + "\n";
                            infotext += @"Area [m" + Gral.Main.SquareString + "]: " + Math.Round(_foundobj.Area) + "\n";
                            AddItemInfoToDrawingObject(infotext, (float)St_F.TxtToDbl(textBox1.Text, false), (float)St_F.TxtToDbl(textBox2.Text, false));
                        }
                        Focus();
                    }
                    break;

                case MouseMode.BuildingSel:
                    //select buildings
                    {
                        int i = 0;
                        int found = -1;
                        BuildingData _foundobj = null;
                        int x1 = 0;
                        int y1 = 0;
                        List<Point> poly = new List<Point>();
                        foreach (BuildingData _bd in EditB.ItemData)
                        {
                            List<PointD> _pt = _bd.Pt;
                            poly.Clear();
                            for (int j = 0; j < _pt.Count; j++)
                            {
                                x1 = Convert.ToInt32((_pt[j].X - MapSize.West) / BmpScale / MapSize.SizeX) + TransformX;
                                y1 = Convert.ToInt32((_pt[j].Y - MapSize.North) / BmpScale / MapSize.SizeY) + TransformY;
                                poly.Add(new Point(x1, y1));
                            }

                            if (St_F.PointInPolygon(new Point(e.X, e.Y), poly))
                            {
                                found = i;
                                _foundobj = _bd;
                                break;
                            }
                            i += 1;
                        }
                        if (found > -1 && _foundobj !=  null)
                        {
                            EditB.SetTrackBar(found + 1);
                            EditB.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditB.FillValues();
                            double height = _foundobj.Height;

                            //Ausgabe der Info in Infobox
                            string infotext = "'" + _foundobj.Name + "'\n";
                            if (height >= 0)
                            {
                                infotext += "Height (rel) [m]: " + Math.Round(_foundobj.Height, 1).ToString() + "\n";
                            }
                            else
                            {
                                infotext += "Height (abs) [m]: " + St_F.DblToIvarTxt(Math.Abs(Math.Round(height, 1))) + "\n";
                            }

                            //infotext += "Lower bound [m]: " + _bd.LowerBound + "\n";
                            infotext += @"Area [m" + Gral.Main.SquareString + "]: " + Math.Round(_foundobj.Area, 1).ToString() + "\n";
                            AddItemInfoToDrawingObject(infotext, (float)St_F.TxtToDbl(textBox1.Text, false), (float)St_F.TxtToDbl(textBox2.Text, false));
                        }
                        Focus();
                    }
                    break;

                case MouseMode.LineSourceSel:
                    //select line sources
                    {
                        int i = 0;
                        int found = -1;
                        LineSourceData _foundobj = null;
                        List<Point> poly = new List<Point>();
                        foreach (LineSourceData _ls in EditLS.ItemData)
                        {
                            poly.Clear();
                            //Point[] poly = new Point[4];

                            for (int j = 0; j < _ls.Pt.Count - 1; j++)
                            {
                                double x1 = (_ls.Pt[j].X - MapSize.West) / BmpScale / MapSize.SizeX + TransformX;
                                double y1 = (_ls.Pt[j].Y - MapSize.North) / BmpScale / MapSize.SizeY + TransformY;
                                double x2 = (_ls.Pt[j + 1].X - MapSize.West) / BmpScale / MapSize.SizeX + TransformX;
                                double y2 = (_ls.Pt[j + 1].Y - MapSize.North) / BmpScale / MapSize.SizeY + TransformY;

                                double length = Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2));
                                if (length == 0)
                                {
                                    length = 0.1;
                                }

                                double cosalpha = (x2 - x1) / length;
                                double sinalpha = (y1 - y2) / length;
                                double dx = Math.Max(_ls.Width / 2 / BmpScale / MapSize.SizeX, 1) * sinalpha;
                                double dy = Math.Max(_ls.Width / 2 / BmpScale / MapSize.SizeX, 1) * cosalpha;
                                poly.Add(new Point(Convert.ToInt32(x1 + dx), Convert.ToInt32(y1 + dy)));
                                poly.Add(new Point(Convert.ToInt32(x1 - dx), Convert.ToInt32(y1 - dy)));
                                poly.Add(new Point(Convert.ToInt32(x2 - dx), Convert.ToInt32(y2 - dy)));
                                poly.Add(new Point(Convert.ToInt32(x2 + dx), Convert.ToInt32(y2 + dy)));

                                if (St_F.PointInPolygon(new Point(e.X, e.Y), poly))
                                {
                                    found = i;
                                    _foundobj = _ls;
                                    break;
                                }
                            }
                            i += 1;
                        }
                        if (found > -1 && _foundobj != null)
                        {
                            EditLS.SetTrackBar(found + 1);
                            EditLS.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditLS.FillValues();
                            
                            //Ausgabe der Info in Infobox
                            string infotext = "'" + _foundobj.Name + "'\n";
                            if (_foundobj.Height >= 0)
                            {
                                infotext += "Height (rel) [m]: \t" + Math.Abs(Math.Round(_foundobj.Height, 1)).ToString() + "\n";
                            }
                            else
                            {
                                infotext += "Height (abs) [m]: \t" + Math.Abs(Math.Round(_foundobj.Height, 1)).ToString() + "\n";
                            }
                            infotext += "Vert. extension [m]: \t" + Math.Round(_foundobj.VerticalExt, 1).ToString() + "\n";
                            infotext += "Width [m]: \t" + Math.Round(_foundobj.Width, 1).ToString() + "\n";

                            if (_foundobj.Nemo.AvDailyTraffic > 0)
                            {
                                infotext += "Veh/Day:            " + _foundobj.Nemo.AvDailyTraffic.ToString() + "\n";
                                infotext += "Heavy Duty Veh [%]: " + _foundobj.Nemo.ShareHDV.ToString() + "\n";
                                infotext += "Slope [%]:          " + _foundobj.Nemo.Slope.ToString() + "\n";
                                infotext += "Reference Year:     " + _foundobj.Nemo.BaseYear.ToString() + "\n";
                                infotext += "Traffic Situation:  " + EditLS.GetSelectedListBox1Item() + "\n";
                            }

                            double length = St_F.CalcLenght(_foundobj.Pt);
                            infotext += "Length [km]: \t" + Convert.ToString(Math.Round(length / 1000, 3)) + "\n";
                            double[] emission = new double[Gral.Main.PollutantList.Count];
                            foreach (PollutantsData _poll in _foundobj.Poll)
                            {
                                for (int r = 0; r < 10; r++)
                                {
                                    int index = Convert.ToInt32(_poll.Pollutant[r]);
                                    try
                                    {
                                        emission[index] += _poll.EmissionRate[r];
                                    }
                                    catch { }
                                }
                            }
                            for (int r = 0; r < Gral.Main.PollutantList.Count; r++)
                            {
                                if (emission[r] > 0)
                                {
                                    if (Gral.Main.PollutantList[r] != "Odour")
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[kg/h/km]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                    else
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[MOU/h/km]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                }
                            }
                            AddItemInfoToDrawingObject(infotext, (float)St_F.TxtToDbl(textBox1.Text, false), (float)St_F.TxtToDbl(textBox2.Text, false));
                        }
                        Focus();
                    }
                    break;

                case MouseMode.PortalSourceSel:
                    //select portal sources
                    {
                        int i = 0;
                        int found = -1;
                        PortalsData _foundobj = null;
                        
                        foreach (PortalsData _po in EditPortals.ItemData)
                        {
                            int sourcegroups = _po.Poll.Count;
                            double x1 = (_po.Pt1.X - MapSize.West) / BmpScale / MapSize.SizeX + TransformX;
                            double y1 = (_po.Pt1.Y - MapSize.North) / BmpScale / MapSize.SizeY + TransformY;
                            double x2 = (_po.Pt2.X - MapSize.West) / BmpScale / MapSize.SizeX + TransformX;
                            double y2 = (_po.Pt2.Y - MapSize.North) / BmpScale / MapSize.SizeY + TransformY;

                            int xmean = Convert.ToInt32((x1 + x2) * 0.5);
                            int ymean = Convert.ToInt32((y1 + y2) * 0.5);
                            if ((e.X >= xmean - 10) && (e.X <= xmean + 10) && (e.Y >= ymean - 10) && (e.Y <= ymean + 10))
                            {
                                found = i;
                                _foundobj = _po;
                                break;
                            }
                            i += 1;
                        }
                        if (found > -1 && _foundobj != null)
                        {
                            EditPortals.SetTrackBar(found + 1);
                            EditPortals.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditPortals.FillValues();

                            //Ausgabe der Info in Infobox
                            string infotext = "'" + _foundobj.Name + "'\n";
                            if (_foundobj.BaseHeight >= 0)
                            {
                                infotext += "Base height (rel)[m]: " + Math.Abs(Math.Round(_foundobj.BaseHeight, 1)).ToString() + "\n";
                            }
                            else
                            {
                                infotext += "Base height (abs)[m]: " + Math.Abs(Math.Round(_foundobj.BaseHeight, 1)).ToString() + "\n";
                            }
                            infotext += "Height [m]: " + Math.Round(_foundobj.Height, 1).ToString() + "\n";
                            int crosssection = Convert.ToInt32(_foundobj.Height * Math.Sqrt(Math.Pow(_foundobj.Pt1.X - _foundobj.Pt2.X, 2) + Math.Pow(_foundobj.Pt1.Y - _foundobj.Pt2.Y, 2)));
                            infotext += "Section [m�]: " + crosssection.ToString() + "\n";
                            if (_foundobj.Direction.Contains("1"))
                            {
                                infotext += "Bidirectional \n";
                            }
                            else
                            {
                                infotext += "Unidirectional \n";
                            }
                            double[] emission = new double[Gral.Main.PollutantList.Count];
                            foreach (PollutantsData _poll in _foundobj.Poll)
                            {
                                for (int r = 0; r < 10; r++)
                                {
                                    int index = Convert.ToInt32(_poll.Pollutant[r]);
                                    try
                                    {
                                        emission[index] += _poll.EmissionRate[r];
                                    }
                                    catch { }
                                }
                            }
                            for (int r = 0; r < Gral.Main.PollutantList.Count; r++)
                            {
                                if (emission[r] > 0)
                                {
                                    if (Gral.Main.PollutantList[r] != "Odour")
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[kg/h]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                    else
                                    {
                                        infotext += Convert.ToString(Gral.Main.PollutantList[r]) + "[MOU/h]: \t" + Convert.ToString(Math.Round(emission[r], 4)) + "\n";
                                    }
                                }
                            }
                            AddItemInfoToDrawingObject(infotext, (float)St_F.TxtToDbl(textBox1.Text, false), (float)St_F.TxtToDbl(textBox2.Text, false));
                            break;
                        }
                        Focus();
                    }
                    break;

                case MouseMode.WallSel:
                    //select walls
                    {
                        int i = 0;
                        int found = -1;
                        WallData _foundobj = null;
                        
                        List<Point> poly = new List<Point>();
                        foreach (WallData _wd in EditWall.ItemData)
                        {
                            poly.Clear();

                            for (int j = 0; j < _wd.Pt.Count - 1; j++)
                            {
                                double x1 = (_wd.Pt[j].X - MapSize.West) / BmpScale / MapSize.SizeX + TransformX;
                                double y1 = (_wd.Pt[j].Y - MapSize.North) / BmpScale / MapSize.SizeY + TransformY;
                                double x2 = (_wd.Pt[j + 1].X - MapSize.West) / BmpScale / MapSize.SizeX + TransformX;
                                double y2 = (_wd.Pt[j + 1].Y - MapSize.North) / BmpScale / MapSize.SizeY + TransformY;

                                double length = Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2));
                                if (length == 0)
                                {
                                    length = 0.1;
                                }

                                double cosalpha = (x2 - x1) / length;
                                double sinalpha = (y1 - y2) / length;
                                double dx = Math.Max(Convert.ToDouble(MainForm.numericUpDown10.Value) / 2 / BmpScale / MapSize.SizeX, 1) * sinalpha;
                                double dy = Math.Max(Convert.ToDouble(MainForm.numericUpDown10.Value) / 2 / BmpScale / MapSize.SizeX, 1) * cosalpha;
                                poly.Add(new Point(Convert.ToInt32(x1 + dx), Convert.ToInt32(y1 + dy)));
                                poly.Add(new Point(Convert.ToInt32(x1 - dx), Convert.ToInt32(y1 - dy)));
                                poly.Add(new Point(Convert.ToInt32(x2 - dx), Convert.ToInt32(y2 - dy)));
                                poly.Add(new Point(Convert.ToInt32(x2 + dx), Convert.ToInt32(y2 + dy)));

                                if (St_F.PointInPolygon(new Point(e.X, e.Y), poly))
                                {
                                    found = i;
                                    _foundobj = _wd;
                                    break;
                                }
                            }
                            i += 1;
                        }
                        if (found > -1 && _foundobj != null)
                        {
                            EditWall.SetTrackBar(found + 1);
                            EditWall.ItemDisplayNr = found;
                            SelectedItems.Add(found);
                            EditWall.FillValues();
                            AddItemInfoToDrawingObject("'" + _foundobj.Name + "'", (float)St_F.TxtToDbl(textBox1.Text, false), (float)St_F.TxtToDbl(textBox2.Text, false));
                        }
                        Picturebox1_Paint(); // 
                        Focus();
                    }
                    break; ;

                case MouseMode.ViewNorthArrowPos:
                    //digitize position of north arrow
                    {
                        //get x,y coordinates
                        NorthArrow.X = e.X;
                        NorthArrow.Y = e.Y;

                        bool exist = false;
                        foreach (DrawingObjects _drobj in ItemOptions)
                        {
                            if (_drobj.Name == "NORTH ARROW")
                            {
                                exist = true;
                                _drobj.ContourLabelDist = (int)(NorthArrow.Scale * 100);
                                break;
                            }
                        }

                        if (exist == false)
                        {
                            DrawingObjects _drobj = new DrawingObjects("NORTH ARROW")
                            {
                                Label = 0,
                                LabelFont = new Font("Arial", 12),
                                ContourLabelDist = (int)(NorthArrow.Scale * 100)
                            };
                            ItemOptions.Insert(0, _drobj);
                        }

                        SaveDomainSettings(1);
                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.ViewScaleBarPos:
                    //digitize position of map scale bar
                    {
                        //get x,y coordinates
                        MapScale.X = e.X;
                        MapScale.Y = e.Y;
                        bool exist = false;
                        foreach (DrawingObjects _drobj in ItemOptions)
                        {
                            if (_drobj.Name.Equals("SCALE BAR"))
                            {
                                exist = true;
                                _drobj.ContourLabelDist = MapScale.Length;
                                break;
                            }
                        }

                        if (exist == false)
                        {
                            DrawingObjects _drobj = new DrawingObjects("SCALE BAR")
                            {
                                Label = 2,
                                ContourLabelDist = MapScale.Length
                            };
                            ItemOptions.Insert(0, _drobj);
                        }

                        SaveDomainSettings(1);
                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.PortalSourcePos:
                    //digitize position of portal source
                    if (Gral.Main.Project_Locked == false)
                    {
                        //get x,y coordinates
                        //get x,y coordinates
                        EditPortals.CornerPortalX[0] = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                        EditPortals.CornerPortalY[0] = Convert.ToDouble(textBox2.Text.Replace(".", decsep));

                        CornerAreaSource[EditLS.CornerLineSource] = new Point(e.X, e.Y);
                        EditLS.CornerLineSource += 1;
                        Graphics g = picturebox1.CreateGraphics();
                        if (EditLS.CornerLineSource > 1)
                        {
                            Pen p = new Pen(Color.LightBlue, 3);
                            g.DrawLine(p, CornerAreaSource[EditLS.CornerLineSource - 2], CornerAreaSource[EditLS.CornerLineSource - 1]);
                            p.Dispose();
                        }
                        Cursor.Clip = Bounds;
                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.ViewDistanceMeasurement:
                    //measuring tool "distance"
                    {
                        //get x,y coordinates
                        CornerAreaSource[EditLS.CornerLineSource] = new Point(e.X, e.Y);
                        EditLS.CornerLineX[EditLS.CornerLineSource] = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                        EditLS.CornerLineY[EditLS.CornerLineSource] = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                        EditLS.CornerLineSource += 1;
                        // Reset Rubber-Line Drawing
                        Cursor.Clip = Bounds;
                        RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.SectionWindSel:
                case MouseMode.SectionConcSel:
                    // select section for windfiled section drawing
                    {
                        //get x,y coordinates
                        if (EditLS.CornerLineSource == 0)
                        {
                            CornerAreaSource[EditLS.CornerLineSource] = new Point(e.X, e.Y);
                        }
                        EditLS.CornerLineX[EditLS.CornerLineSource] = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                        EditLS.CornerLineY[EditLS.CornerLineSource] = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                        EditLS.CornerLineSource = 1;
                        // Reset Rubber-Line Drawing
                        Cursor.Clip = Bounds;
                        RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.ViewAreaMeasurement:
                    //measuring tool "area"
                    {
                        //get x,y coordinates
                        CornerAreaSource[EditAS.CornerAreaCount] = new Point(e.X, e.Y);
                        EditAS.CornerAreaX[EditAS.CornerAreaCount] = Convert.ToDouble(textBox1.Text.Replace(".", decsep));
                        EditAS.CornerAreaY[EditAS.CornerAreaCount] = Convert.ToDouble(textBox2.Text.Replace(".", decsep));
                        EditAS.CornerAreaCount += 1;
                        // Reset Rubber-Line Drawing
                        Cursor.Clip = Bounds;
                        RubberLineCoors[0].X = -1; RubberLineCoors[0].Y = -1;
                        Picturebox1_Paint(); // 
                    }
                    break;

                case MouseMode.ViewLegendPos:
                    //position of legend
                    {
                        if (ActualEditedDrawingObject != null)
                        {
                            string[] dummy = new string[3];
                            dummy = ActualEditedDrawingObject.ColorScale.Split(new char[] { ',' });
                            ActualEditedDrawingObject.ColorScale = Convert.ToString(e.X) + "," + Convert.ToString(e.Y) + "," + dummy[2];
                            Picturebox1_Paint();
                        }
                    }
                    break;

                case MouseMode.GrammDomainEndPoint:
                    // set endpoint of GRAMM model domain with shift button
                    if (shift_key_pressed)
                    {
                        // calculate the GRAMM-Domain
                        int xm = Convert.ToInt32((Convert.ToDouble(textBox1.Text.Replace(".", decsep)) - MapSize.West) / (BmpScale * MapSize.SizeX) + TransformX);
                        int ym = Convert.ToInt32((Convert.ToDouble(textBox2.Text.Replace(".", decsep)) - MapSize.North) / (BmpScale * MapSize.SizeY) + TransformY);

                        int x1 = Math.Min(xm, XDomain);
                        int y1 = Math.Min(ym, YDomain);
                        int x2 = Math.Max(xm, XDomain);
                        int y2 = Math.Max(ym, YDomain);
                        int recwidth = x2 - x1;
                        int recheigth = y2 - y1;
                        GRAMMDomain = new Rectangle(x1, y1, recwidth, recheigth);
                        XDomain = 0;

                        Picturebox1_MouseUp(null, e); // force button up event
                    }
                    break;

                case MouseMode.GrammExportFinal:
                    // set endpoint for exporting GRAMM sub-domain with shift button
                    if (shift_key_pressed)
                    {
                        // calculate the GRAMM-Subdomain
                        int xm = Convert.ToInt32((Convert.ToDouble(textBox1.Text.Replace(".", decsep)) - MapSize.West) / (BmpScale * MapSize.SizeX) + TransformX);
                        int ym = Convert.ToInt32((Convert.ToDouble(textBox2.Text.Replace(".", decsep)) - MapSize.North) / (BmpScale * MapSize.SizeY) + TransformY);

                        int x1 = Math.Min(xm, XDomain);
                        int y1 = Math.Min(ym, YDomain);
                        int x2 = Math.Max(xm, XDomain);
                        int y2 = Math.Max(ym, YDomain);
                        int recwidth = x2 - x1;
                        int recheigth = y2 - y1;
                        GRAMMDomain = new Rectangle(x1, y1, recwidth, recheigth);

                        Picturebox1_MouseUp(null, e); // force button up event
                    }
                    break;

                case MouseMode.GrammDomainStartPoint:
                    //get starting point for drawing GRAMM model domain
                    {
                        XDomain = Convert.ToInt32((Convert.ToDouble(textBox1.Text.Replace(".", decsep)) - MapSize.West) / (BmpScale * MapSize.SizeX) + TransformX);
                        YDomain = Convert.ToInt32((Convert.ToDouble(textBox2.Text.Replace(".", decsep)) - MapSize.North) / (BmpScale * MapSize.SizeY) + TransformY);
                        //							xdomain = e.X;
                        //							ydomain = e.Y;
                        MouseControl = MouseMode.GrammDomainEndPoint;
                        Cursor.Clip = Bounds;
                    }
                    break;

                case MouseMode.GrammExportStart:
                    //get starting point for exporting GRAMM sub-domain
                    {
                        XDomain = Convert.ToInt32((Convert.ToDouble(textBox1.Text.Replace(".", decsep)) - MapSize.West) / (BmpScale * MapSize.SizeX) + TransformX);
                        YDomain = Convert.ToInt32((Convert.ToDouble(textBox2.Text.Replace(".", decsep)) - MapSize.North) / (BmpScale * MapSize.SizeY) + TransformY);
                        GRAMMDomain = new Rectangle(XDomain, YDomain, 0, 0);
                        MouseControl = MouseMode.GrammExportFinal;
                        Cursor.Clip = Bounds;
                    }
                    break;

                case MouseMode.SetPointMetTimeSeries:
                case MouseMode.SetPointConcTimeSeries:
                    break;

                case MouseMode.SetPointReOrder:
                    //get sample point for re-ordering GRAMM windfield to meet observed wind data better
                    {
                        int xDomain = Convert.ToInt32(Convert.ToDouble(textBox1.Text.Replace(".", decsep)));
                        int yDomain = Convert.ToInt32(Convert.ToDouble(textBox2.Text.Replace(".", decsep)));
                        if ((xDomain < MainForm.GrammDomRect.West) || (xDomain > MainForm.GrammDomRect.East) || (yDomain < MainForm.GrammDomRect.South) || (yDomain > MainForm.GrammDomRect.North))
                        {
                            MessageBox.Show(this, "Point is outside GRAMM domain", "GRAL GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MouseControl = MouseMode.Default;
                            Cursor = Cursors.Default;
                            ReorderGrammWindfields(new PointD(Convert.ToDouble(textBox1.Text.Replace(".", decsep)), Convert.ToDouble(textBox2.Text.Replace(".", decsep))));
                        }
                    }
                    break;

                case MouseMode.SetPointMatch:
                    //get sample point for re-ordering GRAMM windfield to meet newly observed wind and stability data at any location within the model domain
                    {
                        if (MMO != null)
                        {
                            XDomain = Convert.ToInt32(Convert.ToDouble(textBox1.Text.Replace(".", decsep)));
                            YDomain = Convert.ToInt32(Convert.ToDouble(textBox2.Text.Replace(".", decsep)));
                            if ((XDomain < MainForm.GrammDomRect.West) || (XDomain > MainForm.GrammDomRect.East) || (YDomain < MainForm.GrammDomRect.South) || (YDomain > MainForm.GrammDomRect.North))
                            {
                                MessageBox.Show(this, "Point is outside GRAMM domain", "GRAL GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                //set new coordinates manually
                                if (MMO.dataGridView1.CurrentCell != null) // Kuntner: check if line does exist!
                                {
                                    int zeilenindex = MMO.dataGridView1.CurrentCell.RowIndex;
                                    MMO.dataGridView1.Rows[zeilenindex].Cells[1].Value = Convert.ToInt32(XDomain);
                                    MMO.dataGridView1.Rows[zeilenindex].Cells[2].Value = Convert.ToInt32(YDomain);
                                    MMO.BringToFront();
                                }

                            }
                        }
                    }
                    break;

                case MouseMode.SetPointSourceApport:
                    //get sample point for computing source apportionment
                    {
                        MouseControl = MouseMode.Default;
                        Cursor = Cursors.Default;
                        SourceApportionment(new PointD(Convert.ToDouble(textBox1.Text.Replace(".", decsep)), Convert.ToDouble(textBox2.Text.Replace(".", decsep))));
                    }
                    break;

                case MouseMode.SetPointConcFile:
                    //get sample point to get a concentration value 
                    {
                        //MouseControl = MouseMode.Default;
                        GetConcentrationFromFile(ConcFilename, new PointD(Convert.ToDouble(textBox1.Text.Replace(".", decsep)), Convert.ToDouble(textBox2.Text.Replace(".", decsep))));
                    }
                    break;

                case MouseMode.SetPointVertWindProfileOnline:
                    //get sample point for vertical profile for GRAMM online evaluations
                    {
                        MouseControl = MouseMode.Default;
                        Cursor = Cursors.Default;
                        VertProfile(new PointD(Convert.ToDouble(textBox1.Text.Replace(".", decsep)), Convert.ToDouble(textBox2.Text.Replace(".", decsep))));
                    }
                    break;

                case MouseMode.SetPointConcProfile:
                    //get sample point for vertical 3D profile of GRAL concentrations
                    {
                        Vert3DConcentration(new PointD(Convert.ToDouble(textBox1.Text.Replace(".", decsep)), Convert.ToDouble(textBox2.Text.Replace(".", decsep))));
                    }
                    break;

                case MouseMode.SetPointVertWindProfile:
                    //get sample point for vertical profile for GRAMM windfields
                    {
                        VertProfile2(new PointD(Convert.ToDouble(textBox1.Text.Replace(".", decsep)), Convert.ToDouble(textBox2.Text.Replace(".", decsep))));
                    }
                    break;

                case MouseMode.SetPointGRAMMGrid:
                    // check single value at GRAMM grid
                    {
                        int sel = 0;
                        foreach (DrawingObjects _drobj in ItemOptions)
                        {
                            if (_drobj.ContourFilename.EndsWith(".scl"))
                            {
                                sel = Convert.ToInt32(Path.GetFileNameWithoutExtension(_drobj.ContourFilename)); // get the number of this file
                                if (sel > 0)
                                {
                                    break; // if first file found
                                }
                            }
                        }

                        ReadSclUstOblClasses reader = new ReadSclUstOblClasses
                        {
                            FileName = Path.Combine(Gral.Main.ProjectName, @"Computation", Convert.ToString(sel).PadLeft(5, '0') + ".scl")
                        };
                        int x1 = 1;
                        int y1 = 1;
                        int xDomain = Convert.ToInt32(Convert.ToDouble(textBox1.Text.Replace(".", decsep)));
                        int yDomain = Convert.ToInt32(Convert.ToDouble(textBox2.Text.Replace(".", decsep)));

                        if (MainForm.textBox13.Text != "")
                        {
                            x1 = Convert.ToInt32(Math.Floor((xDomain - MainForm.GrammDomRect.West) / MainForm.GRAMMHorGridSize));
                            y1 = Convert.ToInt32(Math.Floor((yDomain - MainForm.GrammDomRect.South) / MainForm.GRAMMHorGridSize));
                        }
                        else
                        {
                            x1 = Convert.ToInt32(Math.Floor((xDomain - Convert.ToInt32(MainForm.textBox6.Text)) / Convert.ToDouble(MainForm.numericUpDown10.Value)));
                            y1 = Convert.ToInt32(Math.Floor((yDomain - Convert.ToInt32(MainForm.textBox5.Text)) / Convert.ToDouble(MainForm.numericUpDown10.Value)));
                        }
                        //MessageBox.Show(this, Convert.ToString(x1) +"/" + Convert.ToString(y1));

                        int result = 0;
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            result = reader.ReadSclMean(x1, y1);
                        }
                        else
                        {
                            result = reader.ReadSclFile(x1, y1); // true => reader = OK
                        }

                        if (result > 0)
                        {
                            if (CheckForExistingDrawingObject("ITEM INFO") > 0) // set item info to the top of the object stack
                            {
                                RemoveItemFromItemOptions("ITEM INFO");
                            }
                            AddItemInfoToDrawingObject("Stability: " + Convert.ToString(result), xDomain, yDomain);
                            //MessageBox.Show(this, "Stability: " + Convert.ToString(result), "GRAL GUI - Stability class", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        reader.close();
                    }
                    break;

                case MouseMode.LineSourceInlineEdit:
                    //final corner point of changed line source point
                    SetNewEdgepointLine();
                    break;

                case MouseMode.WallInlineEdit:
                    //final corner point of changed wall
                    SetNewEdgepointWall();
                    break;

                case MouseMode.BuildingInlineEdit:
                    //final corner point of changed building edge point
                    SetNewEdgepointBuilding();
                    break;

                case MouseMode.AreaSourceEditFinal:
                case MouseMode.AreaInlineEdit:
                    //final corner point of changed area source point
                    SetNewEdgepointArea();
                    break;

                case MouseMode.VegetationEditFinal:
                case MouseMode.VegetationInlineEdit:
                    //final corner point of changed vegetation
                    SetNewEdgepointVegetation();
                    break;
            }
        }


        /// <summary>
        /// Add the string a to ItemOptions 
        /// </summary>
        /// <param name="info"></param>
        private void AddItemInfoToDrawingObject(string info, double x1, double y1)
        {
            int index = CheckForExistingDrawingObject("ITEM INFO");
            DrawingObjects _drobj = ItemOptions[index];
            if (_drobj.ShpPoints == null)
            {
                _drobj.ShpPoints = new List<PointF>();
            }
            if (_drobj.ItemInfo == null)
            {
                _drobj.ItemInfo = new List<string>();
            }
            _drobj.ShpPoints.Add(new PointF((float) x1, (float) y1));
            _drobj.ItemInfo.Add(info);
            Picturebox1_Paint();
        }
    }
}