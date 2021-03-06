package DOM;


import java.text.DecimalFormat;

import ij.Prefs;
import ij.gui.GenericDialog;

public class SMLDialog {

	
	int nWidth;  //image width of original image
	int nHeight; //image height of original image
	int nSlices; //number of slices
	
	//finding particles
	int nDetectionType; //task to perform: finding particles, finding and fitting or single molecule
	double dPSFsigma; //standard deviation of PSF approximated by Gaussian
	int nKernelSize; //size of Gaussian kernel for particles enhancement
	double dPixelSize; //size of pixel in nm of original image
	int nThreads; //number of threads for calculation
	int nAreaCut; //threshold of particles area
	boolean bShowParticles; //whether or not add overlay to detected particles
	boolean bIgnoreFP; //whether or not to ignore false positives
	boolean bUseGPUAcceleration; // wheter or not to use GPU acceleration using the OpenCL framework
	int nBatchSize; // number of spots per batch for GPU processing
	int nGroupSize; // size of local workgroups on GPU
	int nIterations; // fixed number of iterations on GPU
	boolean bUseMLE; // fixed number of iterations on GPU

	//reconstructing image
	int nRecParticles;       //what type of particles use for reconstruction 
	double dRecPixelSize;    //pixel size in nm of reconstructed image
	int nRecWidth;           //width of original image for reconstruction	
	int nRecHeight;          //height of original image for reconstruction
	int nIntIndex;           //parameter of intensity reconstruction
	int nSDIndex;            //parameter of intensity reconstruction
	double dFixedSD; 	     //value of SD in case of fixed value
	double dMagnification;   //magnification coefficient
	boolean bCutoff; 	     //whether or not apply cut-off in localization
	double  dcutoff; 	     //cut-off value in original pixels
	boolean bTranslation;    //apply translation during reconstruction
	double dTranslationX;    //translation in X direction 
	double dTranslationY;    //translation in Y direction 
	boolean bFramesInterval; //use all frames (false) or some interval defined by nFrameMin and nFrameMax
	double nFrameMin;
	double nFrameMax;
	boolean bAveragePositions; //average localizations in consecutive frames within 1 pixel for single photoactivation events
	
	
	//drift correction parameters
	boolean bDrift;   //whether or not make drift correction
	int nDriftFrames; //number of frames for averaging per one time interval during drift correction
	int nDriftPixels; //maximal shift in pixels per one time period defined by previous parameter 
	boolean bShowIntermediate;     //show intermediate reconstructions
	boolean bShowCrossCorrelation; //show cross-correlation 
	
	//3D reconstruction parameters
	boolean b3D;				//whether or not to make 3D stack
	double dDistBetweenZSlices;	//z-distance between the slices in the stack
	boolean bCalculateZValues;	//(re)calculate the z-values based on calibration-file
	
	//particle linking parameters
	int nLinkFP;  //what kind of particles use for linking
	double dLinkDistance; //distance between particles to link then
	int nLinkTrace; //whether measure distance from initial spot or 'moving'
	int nLinkFrameGap; //maximum linking gap in frames
	boolean bShowTracks; //whether to show linked tracks or not
	boolean bShowParticlesLink; //show detected particles
	
	//dialog showing options for particle search algorithm		
	public boolean findParticles() {
		String [] DetectionType = new String [] {
				"Detect molecules (no fitting)","Detect molecules and fit", "BALM single molecule intensity calibration", "Fit detected molecules"};
		GenericDialog fpDial = new GenericDialog("Find Particles");
		fpDial.addChoice("Task:", DetectionType, Prefs.get("SiMoLoc.DetectionType", "Detect molecules and fit"));
		fpDial.addNumericField("PSF standard devation, pix", Prefs.get("SiMoLoc.dPSFsigma", 2), 3);
		fpDial.addNumericField("Gaussial kernel size, \nodd number from 7(fast) till 13 (slow)  ", Prefs.get("SiMoLoc.nKernelSize", 7), 0);
		fpDial.addNumericField("Pixel size, nm", Prefs.get("SiMoLoc.dPixelSize", 66), 2);
		fpDial.addNumericField("Number of parallel threads", Prefs.get("SiMoLoc.nThreads", 50), 0);
		fpDial.addCheckbox("Mark detected particles? (better not use this feature on big datasets)", Prefs.get("SiMoLoc.bShowParticles", false));
		fpDial.addCheckbox("Ignore false positives?", Prefs.get("SiMoLoc.bIgnoreFP", false));
		
		fpDial.setInsets(15, 20, 0); // extra space on top
		fpDial.addCheckbox("Accelerate using GPU", Prefs.get("SiMoLoc.bUseGPUAcceleration", false));
		fpDial.addNumericField("Batch size", Prefs.get("SiMoLoc.nBatchSize", 4196), 0);//, 6, "Max : "); //TODO: get maximum value of the GPU
		fpDial.addNumericField("Group size", Prefs.get("SiMoLoc.nGroupSize", 256), 0);//, 6, "Max : ");  //TODO: get maximum value of the GPU
		fpDial.addNumericField("Iterations", Prefs.get("SiMoLoc.nIterations", 10), 0);
		//fpDial.addCheckbox("Use log MLE instead of Chi^2", Prefs.get("SiMoLoc.bUseMLE", false));
		
		fpDial.setResizable(false);
		fpDial.showDialog();
		if (fpDial.wasCanceled())
            return false;
		
		nDetectionType = fpDial.getNextChoiceIndex();
		Prefs.set("SiMoLoc.DetectionType", DetectionType[nDetectionType]);
		dPSFsigma = fpDial.getNextNumber();
		Prefs.set("SiMoLoc.dPSFsigma", dPSFsigma);
		nKernelSize = (int) fpDial.getNextNumber();
		Prefs.set("SiMoLoc.nKernelSize", nKernelSize);
		dPixelSize = fpDial.getNextNumber();
		Prefs.set("SiMoLoc.dPixelSize", dPixelSize);
		nThreads = (int) fpDial.getNextNumber();
		Prefs.set("SiMoLoc.nThreads", nThreads);
		bShowParticles = fpDial.getNextBoolean();
		Prefs.set("SiMoLoc.bShowParticles", bShowParticles);
		bIgnoreFP = fpDial.getNextBoolean();
		Prefs.set("SiMoLoc.bIgnoreFP", bIgnoreFP);
		bUseGPUAcceleration = fpDial.getNextBoolean();
		Prefs.set("SiMoLoc.bUseGPUAcceleration", bUseGPUAcceleration);
		nBatchSize = (int)fpDial.getNextNumber();
		Prefs.set("SiMoLoc.nBatchSize", nBatchSize);
		nGroupSize = (int)fpDial.getNextNumber();
		Prefs.set("SiMoLoc.nGroupSize", nGroupSize);
		nIterations = (int)fpDial.getNextNumber();
		Prefs.set("SiMoLoc.nIterations", nIterations);
		bUseMLE = false;// TODO: set to fpDial.getNextBoolean();
		Prefs.set("SiMoLoc.bUseMLE", bUseMLE);
		
		return true;
	}
	
	public boolean ReconstructImage(double xlocavg_, double ylocavg_, double fminframe, double fmaxframe, int xmax, int ymax) //dialog showing options for reconstruction image		
	{
		GenericDialog dgReconstruct = new GenericDialog("Reconstruct Dataset");
		String [] RecIntOptions = new String [] {
				"Normalized probability", "Integrated spot intensity","Amplitude of Gaussian fitting"};
		String [] RecSDOptions = new String [] {
				"Localization precision","Constant value"};
		String [] RecFPOptions = new String [] {
				"Only true positives","True and half positives", "All particles"};
		
		dgReconstruct.addChoice("For reconstruction use:", RecFPOptions, Prefs.get("SiMoLoc.Rec_FP", "Only true positives"));
		dgReconstruct.addNumericField("Pixel size of reconstructed image, nm", Prefs.get("SiMoLoc.Rec_PixSize", 30), 2);
		dgReconstruct.addMessage("Average localization precision in X: " + new DecimalFormat("#.##").format(xlocavg_) + " nm, in Y: " +  new DecimalFormat("#.##").format(ylocavg_) +" nm.");
		dgReconstruct.addNumericField("Width of original image, px", xmax, 0);
		dgReconstruct.addNumericField("Height of original image, px", ymax, 0);
		dgReconstruct.addChoice("Intensity of spots:", RecIntOptions, Prefs.get("SiMoLoc.Rec_Int", "Normalized probability"));
		dgReconstruct.addChoice("SD of spots:", RecSDOptions, Prefs.get("SiMoLoc.Rec_SD", "Localization precision"));
		dgReconstruct.addNumericField("Value of SD in case of constant (in original pixels):", Prefs.get("SiMoLoc.Rec_SDFixed", 2), 2);
		dgReconstruct.addCheckbox("Cut-off for localization precision:", Prefs.get("SiMoLoc.applycutoff", false));
		dgReconstruct.addNumericField("Cut particles off with localization less than (in original pixels): ", Prefs.get("SiMoLoc.cutoff", 0.3), 2);
		dgReconstruct.addMessage("\n");
		dgReconstruct.addCheckbox("Drift-correction (correlation based):", Prefs.get("SiMoLoc.drift", false));		
		dgReconstruct.addNumericField("Number of frames for averaging:", Prefs.get("SiMoLoc.drift_frames", 1000), 0);
		dgReconstruct.addNumericField("Maximum shift in pixels:", Prefs.get("SiMoLoc.drift_pixels", 10), 0);
		dgReconstruct.addCheckbox("Intermediate reconstructions shown (drift)", Prefs.get("SiMoLoc.drift_intermediate_reconstr", false));		
		dgReconstruct.addCheckbox("Cross-correlation images shown (drift)", Prefs.get("SiMoLoc.drift_cross_correlation", false));
		//dgReconstruct.addMessage("\n");
		dgReconstruct.addMessage("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
		dgReconstruct.addCheckbox("3D-reconstruction", Prefs.get("SiMoLoc.create3DStack", false));
		dgReconstruct.addNumericField("Z-distance between slices (nm):", Prefs.get("SiMoLoc.distZSlices", 25), 0);
		dgReconstruct.addCheckbox("Recalculate z-values based on calibration-file", Prefs.get("SiMoLoc.recalZvalues", false));
		dgReconstruct.addMessage("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
		//dgReconstruct.addMessage("\n");
		dgReconstruct.addCheckbox("Reconstruct with translation (in original pixels):", Prefs.get("SiMoLoc.bTranslate", false));
		dgReconstruct.addNumericField("X_Offset:", Prefs.get("SiMoLoc.dTransX", 0), 4);
		dgReconstruct.addNumericField("Y_Offset:", Prefs.get("SiMoLoc.dTransY", 0), 4);
		dgReconstruct.addMessage("\n");
		dgReconstruct.addCheckbox("Use frame interval:", Prefs.get("SiMoLoc.bFramesInterval", false));
		dgReconstruct.addMessage("Current total frame range is from " + new DecimalFormat("#").format(fminframe) + " till " +  new DecimalFormat("#").format(fmaxframe));
		dgReconstruct.addNumericField("Initial frame:", Prefs.get("SiMoLoc.nFrameMin", fminframe), 0);
		dgReconstruct.addNumericField("Last frame:", Prefs.get("SiMoLoc.nFrameMax", fmaxframe), 0);
		dgReconstruct.addMessage("\n");
		dgReconstruct.addCheckbox("Average localizations in consecutive frames within 1 pixel?", Prefs.get("SiMoLoc.bAveragePositions", false));
		
		
		dgReconstruct.showDialog();
		if (dgReconstruct.wasCanceled())
            return false;
		
		nRecParticles = dgReconstruct.getNextChoiceIndex();
		Prefs.set("SiMoLoc.Rec_FP", RecFPOptions[nRecParticles]);
        dRecPixelSize = dgReconstruct.getNextNumber();
        Prefs.set("SiMoLoc.Rec_PixSize", dRecPixelSize);
		nRecWidth = (int) dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.Rec_ImWidth", nRecWidth);
		nRecHeight = (int) dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.Rec_ImHeight", nRecHeight);
		nIntIndex = dgReconstruct.getNextChoiceIndex();
		Prefs.set("SiMoLoc.Rec_Int", RecIntOptions[nIntIndex]);
		nSDIndex = dgReconstruct.getNextChoiceIndex();
		Prefs.set("SiMoLoc.Rec_SD", RecSDOptions[nSDIndex]);
		dFixedSD = dgReconstruct.getNextNumber();
	    Prefs.set("SiMoLoc.Rec_SDFixed", dFixedSD);
		bCutoff = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.applycutoff", bCutoff);
		dcutoff = dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.cutoff", dcutoff);
		bDrift = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.drift", bDrift);
		nDriftFrames = (int) dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.drift_frames", nDriftFrames);
		nDriftPixels = (int) dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.drift_pixels", nDriftPixels);
		bShowIntermediate = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.drift_intermediate_reconstr", bShowIntermediate);
		bShowCrossCorrelation = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.drift_cross_correlation", bShowCrossCorrelation);
		
		//values for 3D reconstruction
		b3D= dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.create3DStack", b3D);
		dDistBetweenZSlices =  dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.distZSlices", dDistBetweenZSlices);
		bCalculateZValues = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.recalZvalues", bCalculateZValues);
		
		bTranslation = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.bTranslate", bTranslation);
		dTranslationX =  dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.dTransX", dTranslationX);
		dTranslationY =  dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.dTransY", dTranslationY);
		bFramesInterval = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.bFramesInterval", bFramesInterval);
		nFrameMin =  dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.nFrameMin", nFrameMin);
		nFrameMax =  dgReconstruct.getNextNumber();
		Prefs.set("SiMoLoc.nFrameMax", nFrameMax);	
		bAveragePositions = dgReconstruct.getNextBoolean();
		Prefs.set("SiMoLoc.bAveragePositions", bAveragePositions);
		return true;
	}
	
	public boolean LinkParticles() //dialog showing options for linking particles after detection
	{
		GenericDialog dgLink = new GenericDialog("Link Particles");
		String [] Link_Dist = new String [] {
				"Initial position", "Next detected position"};
		String [] LinkFPOptions = new String [] {
				"Only true positives","True and half positives", "All particles"};
		
		dgLink.addChoice("For linking use:", LinkFPOptions, Prefs.get("SiMoLoc.Link_FP", "Only true positives"));
		dgLink.addNumericField("Distance between particles for linking, px", Prefs.get("SiMoLoc.LinkDist", 1), 2);
		dgLink.addChoice("Measure distance from:", Link_Dist, Prefs.get("SiMoLoc.LinkTrace", "Initial position"));
		dgLink.addNumericField("Maximum linking closing gap in frames:", Prefs.get("SiMoLoc.LinkFrames", 0), 0);
		dgLink.addCheckbox("Display tracks in overlay?", Prefs.get("SiMoLoc.bShowTracks", true));
		dgLink.addCheckbox("Show detected particles?", Prefs.get("SiMoLoc.bShowParticlesLink", false));
		dgLink.showDialog();
		if (dgLink.wasCanceled())
            return false;
		
		nLinkFP = dgLink.getNextChoiceIndex();
		Prefs.set("SiMoLoc.Link_FP", LinkFPOptions[nLinkFP]);
		dLinkDistance= dgLink.getNextNumber();
		Prefs.set("SiMoLoc.LinkDist", dLinkDistance);
		nLinkTrace = dgLink.getNextChoiceIndex();
		Prefs.set("SiMoLoc.LinkTrace", Link_Dist[nLinkTrace]);
		nLinkFrameGap= (int)dgLink.getNextNumber();
		Prefs.set("SiMoLoc.LinkFrames", nLinkFrameGap);
		bShowTracks = dgLink.getNextBoolean();
		Prefs.set("SiMoLoc.bShowTracks", bShowTracks);
		bShowParticlesLink = dgLink.getNextBoolean();
		Prefs.set("SiMoLoc.bShowParticlesLink", bShowParticlesLink);
		return true;		
	}

}
