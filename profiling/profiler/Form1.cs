﻿using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BitMiracle.LibTiff.Classic;
using Cloo;
using profiler.io;
using profiler.utils;

namespace profiler
{
    public partial class Form1 : Form
    {
        private ComputePlatform _selectedComputePlatform;
        private ComputeDevice _selectedComputeDevice;

        private int _imageDimensionX;
        private int _imageDimensionY;
        private int _imageDimensionZ;

        private String _sourceFilename = String.Empty;
        private String _saveFilename = String.Empty;

        private String _sourceDir = String.Empty;
        private String _saveDir = String.Empty;

        public Form1()
        {
            _imageDimensionZ = 0;
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            TopMost = true;

            // Load platforms
            ReadOnlyCollection<ComputePlatform> platforms = ComputePlatform.Platforms;

            PlatformCombox.DataSource = platforms.ToList();
            PlatformCombox.DisplayMember = "Name";

            comboBoxTransform.DataSource = Enum.GetValues(typeof(TransformationType));

            //TODO need to be removed, nor for testing {
//            _sourceFilename = "..\\..\\..\\data\\fluorophores_radial_45_label_500_persistence_length_2000.csv";
            _sourceFilename = "..\\..\\..\\data\\fluorophores_1k.csv";
            textBoxSelectSourceFile.Text = _sourceFilename;
            _saveFilename = "..\\..\\..\\data\\fluorophores_1k_GPU.tif";
            labelSaveOutputFile.Text = _saveFilename;
            comboBoxTransform.SelectedItem = TransformationType.Affine;
            //TODO }
        }

        private void PlatformCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            _selectedComputePlatform = (ComputePlatform) comboBox.SelectedItem;

            DeviceCombox.DataSource = _selectedComputePlatform.Devices.ToList();
            DeviceCombox.DisplayMember = "Name";

            Console.WriteLine("Selected platform : " + _selectedComputePlatform.Name);
        }

        private void DeviceCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            var selectedDevice = (ComputeDevice) comboBox.SelectedItem;

            Console.WriteLine("Selected device : " + selectedDevice.Name);

            _selectedComputePlatform = (ComputePlatform)PlatformCombox.SelectedItem;
            _selectedComputeDevice = (ComputeDevice)DeviceCombox.SelectedItem;
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            SetOutputDimensions();

            // construct context
            var context = new ComputeContext(_selectedComputeDevice.Type, new ComputeContextPropertyList(_selectedComputePlatform), null, IntPtr.Zero);

            CalculateConvolution(context);
        }

        private void SetOutputDimensions()
        {
            if (TextBoxImageDimensionX.Text == String.Empty || TextBoxImageDimensionY.Text == String.Empty ||
                TextBoxImageDimensionZ.Text == String.Empty) throw new ArgumentOutOfRangeException(String.Format("Dimensions need to be set"));

            _imageDimensionX = Convert.ToInt32(TextBoxImageDimensionX.Text);
            _imageDimensionY = Convert.ToInt32(TextBoxImageDimensionY.Text);
            _imageDimensionZ = Convert.ToInt32(TextBoxImageDimensionZ.Text);
        }

        private void CalculateConvolution(ComputeContext computeContext)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            float dx;
            bool shiftXParse = float.TryParse(textBoxShiftX.Text, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out dx);
            if (!shiftXParse)
                throw new SyntaxErrorException(", needs to be .");

            float dy;
            bool shiftYParse = float.TryParse(textBoxShiftX.Text, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out dy);
            if (!shiftYParse)
                throw new SyntaxErrorException(", needs to be  .");

            float dz;
            bool shiftZParse = float.TryParse(textBoxShiftX.Text, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out dz);
            if (!shiftZParse)
                throw new SyntaxErrorException(", needs to be  .");

            int pixelCount = _imageDimensionX*_imageDimensionY*_imageDimensionZ;

            Console.WriteLine("Computing...");
            Console.WriteLine("Reading kernel...");

            String kernelPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            String kernelString;
            using (var sr = new StreamReader(kernelPath + "\\convolution.cl"))
                kernelString = sr.ReadToEnd();
            
            Console.WriteLine("Reading kernel... done");

            float[] selectedTransformation = Transformations.GetTransformation((TransformationType)comboBoxTransform.SelectedItem, 1.0f / float.Parse(textBoxPixelSize.Text), 1.0f / float.Parse(textBoxPixelSize.Text), 1.0f / float.Parse(textBoxPixelSize.Text), dx, dy, dz);

            //create openCL program
            ComputeProgram computeProgram = new ComputeProgram(computeContext, kernelString);
            
            computeProgram.Build(computeContext.Devices, null, null, IntPtr.Zero);

            ComputeProgramBuildStatus computeProgramBuildStatus = computeProgram.GetBuildStatus(_selectedComputeDevice);
            Console.WriteLine("computeProgramBuildStatus\n\t"+computeProgramBuildStatus);

            String buildLog = computeProgram.GetBuildLog(_selectedComputeDevice);
            Console.WriteLine("buildLog");
            if (buildLog.Equals("\n"))
                Console.WriteLine("\tbuildLog is empty...");
            else
                Console.WriteLine("\t" + buildLog);
            

            float[] fluorophores = CsvData.ReadFluorophores(_sourceFilename);

/////////////////////////////////////////////
// Create a Command Queue & Event List
/////////////////////////////////////////////
            ComputeCommandQueue computeCommandQueue = new ComputeCommandQueue(computeContext, _selectedComputeDevice, ComputeCommandQueueFlags.None);

////////////////////////////////////////////////////////////////
// Create Buffers Transform
////////////////////////////////////////////////////////////////
            ComputeBuffer<float> fluorophoresCoords = new ComputeBuffer<float>(computeContext, ComputeMemoryFlags.ReadWrite, fluorophores.LongLength);

            ComputeBuffer<float> transformationMatrix = new ComputeBuffer<float>(computeContext, ComputeMemoryFlags.ReadOnly, selectedTransformation.LongLength);

/////////////////////////////////////////////
// Create the transformFluorophoresKernel
///////////////////////////////////////////////////////////
            ComputeKernel transformFluorophoresKernel = computeProgram.CreateKernel("transform_fluorophores");
            
/////////////////////////////////////////////
// Set the transformFluorophoresKernel arguments
/////////////////////////////////////////////
            transformFluorophoresKernel.SetMemoryArgument(0, fluorophoresCoords);
            transformFluorophoresKernel.SetMemoryArgument(1, transformationMatrix);

/////////////////////////////////////////////
// Configure the work-item structure
/////////////////////////////////////////////
            long[] globalWorkOffsetTransformFluorophoresKernel = null;
            long[] globalWorkSizeTransformFluorophoresKernel = new long[]   { fluorophores.Length / 4 };
            long[] localWorkSizeTransformFluorophoresKernel = null;

////////////////////////////////////////////////////////
// Enqueue the transformFluorophoresKernel for execution
////////////////////////////////////////////////////////

            computeCommandQueue.WriteToBuffer(fluorophores, fluorophoresCoords, true, null);
            computeCommandQueue.WriteToBuffer(selectedTransformation, transformationMatrix, true, null);
            
            computeCommandQueue.Execute(transformFluorophoresKernel, globalWorkOffsetTransformFluorophoresKernel, globalWorkSizeTransformFluorophoresKernel, localWorkSizeTransformFluorophoresKernel, null);
//            computeCommandQueue.ExecuteTask(transformFluorophoresKernel, transformFluorophoresEvents);

            float[] transformedFluorophores = new float[fluorophores.Length];

            computeCommandQueue.ReadFromBuffer(fluorophoresCoords, ref transformedFluorophores, true, null);
            
            computeCommandQueue.Finish();

            //TODO remove, only for testing
//            for (int i = 0; i < transformedFluorophores.Length; i++)
//            {
//                Console.WriteLine(transformedFluorophores[i]);
//            }
            // /TODO remove, only for testing

            stopwatch.Stop();
            Console.WriteLine("Transform fluophores duration:\n\t" + stopwatch.Elapsed);
            stopwatch.Reset();
            stopwatch.Start();
            // fluorophoresCoords are now transformed (done in place)
            
////////////////////////////////////////////////////////////////
// Create Buffers Convolve Fluorophores
////////////////////////////////////////////////////////////////

            const int convolve_kernel_lwgs = 16;
            int totalBuffer = (int) Math.Ceiling(pixelCount / (float)convolve_kernel_lwgs) * convolve_kernel_lwgs;

            ComputeBuffer<float> resultImage = new ComputeBuffer<float>(computeContext, ComputeMemoryFlags.WriteOnly, totalBuffer);

/////////////////////////////////////////////
// Create the transformFluorophoresKernel
/////////////////////////////////////////////
            ComputeKernel convolveFluorophoresKernel = computeProgram.CreateKernel("convolve_fluorophores");

/////////////////////////////////////////////
// Set the convolveFluorophoresKernel arguments
/////////////////////////////////////////////

            convolveFluorophoresKernel.SetMemoryArgument(0, resultImage);
            convolveFluorophoresKernel.SetValueArgument(1, _imageDimensionX);
            convolveFluorophoresKernel.SetValueArgument(2, _imageDimensionY);
            convolveFluorophoresKernel.SetMemoryArgument(3, fluorophoresCoords);
            convolveFluorophoresKernel.SetLocalArgument(4, convolve_kernel_lwgs);
            convolveFluorophoresKernel.SetValueArgument(5, fluorophores.Length / 4);

/////////////////////////////////////////////
// Configure the work-item structure
/////////////////////////////////////////////
            long[] globalWorkOffsetTransformConvolveFluorophoresKernel = null;
            long[] globalWorkSizeTransformConvolveFluorophoresKernel = new long[] { pixelCount };
            long[] localWorkSizeTransformConvolveFluorophoresKernel = new long[] {convolve_kernel_lwgs};
            
////////////////////////////////////////////////////////
// Enqueue the convolveFluorophoresKernel for execution
////////////////////////////////////////////////////////
            
            computeCommandQueue.Execute(convolveFluorophoresKernel, globalWorkOffsetTransformConvolveFluorophoresKernel, globalWorkSizeTransformConvolveFluorophoresKernel, localWorkSizeTransformConvolveFluorophoresKernel, null);

            float[] resultImageData = new float[totalBuffer];
            computeCommandQueue.ReadFromBuffer(resultImage, ref resultImageData, true, null);

            computeCommandQueue.Finish();

            for (int i = 0; i < pixelCount; i++)
            {
                Console.WriteLine(resultImageData[i]);
            }

            Console.WriteLine("Writing data to file...");
//            CsvData.WriteToDisk("..\\..\\..\\output.csv", resultImageData);
            TiffData.WriteToDisk(resultImageData, _saveFilename, _imageDimensionX, _imageDimensionY);

            Bitmap bitmap = new Bitmap(_imageDimensionX, _imageDimensionY);

            float max = resultImageData.Max();

            float scale = 255/(float)max;

//            for (int r = 0; r < _imageDimensionY; r++)
//            {
//                for (int c = 0; c < _imageDimensionX; c++)
//                {
//                    float value = resultImageData[c*(r + 1)];
//                    Color newColor = Color.FromArgb((int)(value * scale), (int)(value * scale), (int)(value * scale));
//                    bitmap.SetPixel(c,r, newColor);
//                }
//            }

            ushort[] ushortdata = new ushort[resultImageData.Length];

            for (int i = 0; i < resultImageData.Length; i++)
            {
                ushortdata[i] = (ushort)resultImageData[i];
            }

            uint[] convertGray16ToRgb = ConvertGray16ToRGB(ushortdata, 16);

            byte[] bytes = new byte[convertGray16ToRgb.Length * 4];
//
//            int[] resultImageData2 = new int[resultImageData.Length];
//
            for (int index = 0; index < convertGray16ToRgb.Length; index++)
            {
//                resultImageData2[index] = (int)(scale*resultImageData[index]);

                byte[] bytes1 = BitConverter.GetBytes(convertGray16ToRgb[index]);
                bytes[index] = bytes1[0];
                bytes[4 * index + 1] = bytes1[1];
                bytes[4 * index + 2] = bytes1[2];
                bytes[4 * index + 3] = bytes1[3];
            }
//
//            for (int r = 0; r < _imageDimensionY; r++)
//            {
//                for (int c = 0; c < _imageDimensionX; c++)
//                {
//                    float value = resultImageData2[c*(r + 1)];
//                    Color newColor = Color.FromArgb((int)(value), (int)(value), (int)(value));
//                    bitmap.SetPixel(c,r, newColor);
//                }
//            }
//            bitmap.Save("c:\\temp.bmp");


            using (MemoryStream ms = new MemoryStream(bytes))
            {
                Image image = Bitmap.FromStream(ms);
                image.Save("c:\\temp.bmp");
            }
            
            Console.WriteLine("Writing data to file... done");

            stopwatch.Stop();
            Console.WriteLine("Convolve fluophores duration:\n\t" + stopwatch.Elapsed);
            Console.WriteLine("Computing... done");
        }

        /// <summary>Convert 16 bit gray to RGB</summary>
        /// <param name="grayPixels">source pixels</param>
        /// <param name="bitsStored">bits used [1..16]</param>
        /// <returns>rgb pixels</returns>
        private uint[] ConvertGray16ToRGB(ushort[] grayPixels, int bitsUsed)
        {
            int pixelCount = grayPixels.Length;
            uint[] rgbPixels = new uint[pixelCount];
            int shift = bitsUsed - 8;
            for (int i = 0; i < pixelCount; i++)
            {
                uint gray = (uint)grayPixels[i] >> shift;
                rgbPixels[i] = 0xff000000U | gray | (gray << 8) | (gray <<
                16);
            }
            return (rgbPixels);
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            var fdlg = new OpenFileDialog
            {
                Title = "C# Corner Open File Dialog",
                InitialDirectory = @"c:\",
                Filter = "Image files (*.csv) | *.csv; *.tiff; *.TIF; *.TIFF;",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textBoxSelectSourceFile.Text = fdlg.FileName;
                _sourceFilename = fdlg.FileName;

                Console.WriteLine("Source file set to: \n\t" + _sourceFilename);
            }
        }
        
        private void radioButtonSingleFile_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxSingleFile.Enabled = true;
            groupBoxMultipleFiles.Enabled = false;

            RadioButton radioButton = (RadioButton) sender;
            if (radioButton.Checked)
            {
                Console.WriteLine("Single file mode selected");
            }
        }

        private void radioButtonMultipleFiles_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxSingleFile.Enabled = false;
            groupBoxMultipleFiles.Enabled = true;
            
            RadioButton radioButton = (RadioButton) sender;
            if (radioButton.Checked)
            {
                Console.WriteLine("Multiple files mode selected");
            }
        }

        private void buttonSaveOutputFile_Click(object sender, EventArgs e)
        {
            var fdlg = new SaveFileDialog
            {
                Title = "Save TIFF Data File",
                InitialDirectory = @"c:\",
                Filter = "Image files (*.tif, *.tiff) | *.tiff; *.tiff; *.TIF; *.TIFF;",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                labelSaveOutputFile.Text = fdlg.FileName;
                _saveFilename = fdlg.FileName;

                Console.WriteLine("Save file set to: \n\t" + _saveFilename);
            }
        }

        private void buttonSelectDir_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                labelSelectSourceDir.Text = fbd.SelectedPath;
                _sourceDir = fbd.SelectedPath;

                Console.WriteLine("Source dir set to: \n\t" + _sourceDir);
            }
        }

        private void buttonSaveOutputDir_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                labelSaveOutputDir.Text = fbd.SelectedPath;
                _saveDir = fbd.SelectedPath;

                Console.WriteLine("Save dir set to: \n\t" + _saveDir);
            }
        }

        private void comboBoxTransform_SelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("TransformationType type set to: \n\t" + ((ComboBox)sender).SelectedItem);
        }

        private void buttonListDeviceInfo_Click(object sender, EventArgs e)
        {
            foreach (var prop in _selectedComputeDevice.GetType().GetProperties())
            {
                Console.WriteLine(prop.Name + " = " + prop.GetValue(_selectedComputeDevice, null));
            }
        }
    }
}