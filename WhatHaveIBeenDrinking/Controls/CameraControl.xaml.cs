﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace WhatHaveIBeenDrinking.Controls
{
    public enum AutoCaptureState
    {
        WaitingForFaces,
        WaitingForStillFaces,
        ShowingCountdownForCapture,
        ShowingCapturedPhoto
    }

    public class FaceDetectedEventArgs
    {
        public DetectedFace Face;
        public SoftwareBitmap Bitmap;
        //Face GetLastFaceAttributesForFace(BitmapBounds faceBox);
        //IdentifiedPerson GetLastIdentifiedPersonForFace(BitmapBounds faceBox);
        //SimilarPersistedFace GetLastSimilarPersistedFaceForFace(BitmapBounds faceBox);
    }

    public sealed partial class CameraControl : UserControl
    {
        public event EventHandler<FaceDetectedEventArgs> FaceDetected;

        public event EventHandler FaceDetectionStarted;
        public event EventHandler FacesNoLongerDetected;

        public event EventHandler<AutoCaptureState> AutoCaptureStateChanged;
        public event EventHandler CameraRestarted;
        public event EventHandler CameraAspectRatioChanged;

        public static readonly DependencyProperty ShowDialogOnApiErrorsProperty =
            DependencyProperty.Register(
            "ShowDialogOnApiErrors",
            typeof(bool),
            typeof(CameraControl),
            new PropertyMetadata(true)
            );

        private List<BitmapBounds> ActiveFaces = new List<BitmapBounds>();

        public bool FilterOutSmallFaces
        {
            get;
            set;
        }

        private bool enableAutoCaptureMode;
        public bool EnableAutoCaptureMode
        {
            get
            {
                return enableAutoCaptureMode;
            }
            set
            {
                this.enableAutoCaptureMode = value;
                //this.commandBar.Visibility = this.enableAutoCaptureMode ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public double CameraAspectRatio { get; set; }
        public int CameraResolutionWidth { get; private set; }
        public int CameraResolutionHeight { get; private set; }

        public int NumFacesOnLastFrame { get; set; }

        public CameraStreamState CameraStreamState { get { return captureManager != null ? captureManager.CameraStreamState : CameraStreamState.NotStreaming; } }

        private MediaCapture captureManager;
        private VideoEncodingProperties videoProperties;
        private FaceTracker faceTracker;
        private ThreadPoolTimer frameProcessingTimer;
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);
        private AutoCaptureState autoCaptureState;
        private IEnumerable<DetectedFace> detectedFacesFromPreviousFrame;
        private DateTime timeSinceWaitingForStill;
        private DateTime lastTimeWhenAFaceWasDetected;

        public CameraControl()
        {
            this.InitializeComponent();
        }


        public async Task StartStreamAsync(bool isForRealTimeProcessing = false)
        {
            try
            {
                if (captureManager == null ||
                    captureManager.CameraStreamState == CameraStreamState.Shutdown ||
                    captureManager.CameraStreamState == CameraStreamState.NotStreaming)
                {
                    if (captureManager != null)
                    {
                        captureManager.Dispose();
                    }

                    captureManager = new MediaCapture();

                    MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                    var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                    //JL Default to first camera
                    var selectedCamera = allCameras.FirstOrDefault();
                    if (selectedCamera != null)
                    {
                        settings.VideoDeviceId = selectedCamera.Id;
                    }

                    await captureManager.InitializeAsync(settings);
                    await SetVideoEncodingToHighestResolution(isForRealTimeProcessing);

                    this.webCamCaptureElement.Source = captureManager;
                }

                if (captureManager.CameraStreamState == CameraStreamState.NotStreaming)
                {
                    if (faceTracker == null)
                    {
                        faceTracker = await FaceTracker.CreateAsync();
                    }

                    videoProperties = this.captureManager.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                    await captureManager.StartPreviewAsync();

                    if (frameProcessingTimer != null)
                    {
                        frameProcessingTimer.Cancel();
                        frameProcessingSemaphore.Release();
                    }
                    TimeSpan timerInterval = TimeSpan.FromMilliseconds(250); //15fps
                    frameProcessingTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);

                    webCamCaptureElement.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                //await Util.GenericApiCallExceptionHandler(ex, "Error starting the camera.");
            }
        }

        private async Task SetVideoEncodingToHighestResolution(bool isForRealTimeProcessing = false)
        {
            VideoEncodingProperties highestVideoEncodingSetting;

            // Sort the available resolutions from highest to lowest
            var availableResolutions = this.captureManager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Cast<VideoEncodingProperties>().OrderByDescending(v => v.Width * v.Height * (v.FrameRate.Numerator / v.FrameRate.Denominator));

            if (isForRealTimeProcessing)
            {
                uint maxHeightForRealTime = 1080;
                // Find the highest resolution that is 720p or lower
                highestVideoEncodingSetting = availableResolutions.FirstOrDefault(v => v.Height <= maxHeightForRealTime);
                if (highestVideoEncodingSetting == null)
                {
                    // Since we didn't find 720p or lower, look for the first up from there
                    highestVideoEncodingSetting = availableResolutions.LastOrDefault();
                }
            }
            else
            {
                // Use the highest resolution
                highestVideoEncodingSetting = availableResolutions.FirstOrDefault();
            }

            if (highestVideoEncodingSetting != null)
            {
                this.CameraAspectRatio = (double)highestVideoEncodingSetting.Width / (double)highestVideoEncodingSetting.Height;
                this.CameraResolutionHeight = (int)highestVideoEncodingSetting.Height;
                this.CameraResolutionWidth = (int)highestVideoEncodingSetting.Width;

                await this.captureManager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, highestVideoEncodingSetting);

                if (this.CameraAspectRatioChanged != null)
                {
                    this.CameraAspectRatioChanged(this, EventArgs.Empty);
                }
            }
        }


        public async Task<SoftwareBitmap> GetFrame()
        {
            if (captureManager.CameraStreamState != CameraStreamState.Streaming || !frameProcessingSemaphore.Wait(500))
            {
                return null;
            }

            try
            {
                // Create a VideoFrame object specifying the pixel format we want our capture image to be (NV12 bitmap in this case).
                // GetPreviewFrame will convert the native webcam frame into this format.
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Rgba8;

                using (VideoFrame currentFrame = new VideoFrame(InputPixelFormat, (int)videoProperties.Width, (int)videoProperties.Height))
                {
                    await captureManager.GetPreviewFrameAsync(currentFrame);
                    
                    // Create our visualization using the frame dimensions and face results but run it on the UI thread.
                    var currentFrameSize = new Windows.Foundation.Size(currentFrame.SoftwareBitmap.PixelWidth, currentFrame.SoftwareBitmap.PixelHeight);

                    var rgbaBitmap = SoftwareBitmap.Convert(currentFrame.SoftwareBitmap, BitmapPixelFormat.Rgba8);

                    return rgbaBitmap;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                frameProcessingSemaphore.Release();
            }

            return null;
        }


        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {
            if (captureManager.CameraStreamState != CameraStreamState.Streaming
                || !frameProcessingSemaphore.Wait(0))
            {
                return;
            }

            try
            {
                IEnumerable<DetectedFace> faces = null;

                // Create a VideoFrame object specifying the pixel format we want our capture image to be (NV12 bitmap in this case).
                // GetPreviewFrame will convert the native webcam frame into this format.
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;

                using (VideoFrame currentFrame = new VideoFrame(InputPixelFormat, (int)videoProperties.Width, (int)videoProperties.Height))
                {
                    await captureManager.GetPreviewFrameAsync(currentFrame);

                    // The returned VideoFrame should be in the supported NV12 format but we need to verify this.
                    if (FaceDetector.IsBitmapPixelFormatSupported(currentFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        faces = await faceTracker.ProcessNextFrameAsync(currentFrame);

                        if (FilterOutSmallFaces)
                        {
                            // We filter out small faces here. 
                            faces = faces.Where(f => CoreUtil.IsFaceBigEnoughForDetection((int)f.FaceBox.Height, (int)videoProperties.Height));
                        }

                        NumFacesOnLastFrame = faces.Count();

                        if (EnableAutoCaptureMode)
                        {
                            UpdateAutoCaptureState(faces);
                        }

                        // Create our visualization using the frame dimensions and face results but run it on the UI thread.
                        var currentFrameSize = new Windows.Foundation.Size(currentFrame.SoftwareBitmap.PixelWidth, currentFrame.SoftwareBitmap.PixelHeight);

                        var rgbaBitmap = SoftwareBitmap.Convert(currentFrame.SoftwareBitmap, BitmapPixelFormat.Rgba8);

                        HandleFaces(currentFrameSize, faces, rgbaBitmap);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                frameProcessingSemaphore.Release();
            }
        }


        private void HandleFaces(Windows.Foundation.Size framePixelSize, IEnumerable<DetectedFace> detectedFaces, SoftwareBitmap currentFrame)
        {
            //var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //{
            //    FaceTrackingVisualizationCanvas.Children.Clear();
            //});
            

            //double actualWidth = FaceTrackingVisualizationCanvas.ActualWidth;
            //double actualHeight = FaceTrackingVisualizationCanvas.ActualHeight;

            if (captureManager.CameraStreamState == CameraStreamState.Streaming &&
                detectedFaces != null /*&& actualWidth != 0 && actualHeight != 0*/)
            {
                //double widthScale = framePixelSize.Width / actualWidth;
                //double heightScale = framePixelSize.Height / actualHeight;

                List<BitmapBounds> currentFaces = new List<BitmapBounds>();
                List<BitmapBounds> missingFaces = new List<BitmapBounds>();

                foreach (DetectedFace detectedFace in detectedFaces)
                {
                    //RealTimeFaceIdentificationBorder faceBorder = new RealTimeFaceIdentificationBorder();
                    //this.FaceTrackingVisualizationCanvas.Children.Add(faceBorder);

                    //faceBorder.ShowFaceRectangle((uint)(detectedFace.FaceBox.X / widthScale), (uint)(detectedFace.FaceBox.Y / heightScale), (uint)(detectedFace.FaceBox.Width / widthScale), (uint)(detectedFace.FaceBox.Height / heightScale));

                    if (currentFrame == null)
                    {
                        continue;
                    }

                    var potentialMatches = ActiveFaces.Where(existingFace =>
                    {
                        return CoreUtil.AreFacesPotentiallyTheSame(existingFace, detectedFace.FaceBox);
                    });

                    // Is this a new face to us?
                    if (potentialMatches.Count() == 0)
                    {
                        var eventArgs = new FaceDetectedEventArgs()
                        {
                            Face = detectedFace,
                            Bitmap = currentFrame
                        };

                        if (!ActiveFaces.Any())
                        {
                            FaceDetectionStarted?.Invoke(this, new EventArgs());
                        }

                        FaceDetected?.Invoke(this, eventArgs);
                    }

                    currentFaces.Add(detectedFace.FaceBox);
                }

                if (currentFaces.Count == 0)
                {
                    FacesNoLongerDetected?.Invoke(this, new EventArgs());
                }

                ActiveFaces = currentFaces;
            }
        }

        private async void UpdateAutoCaptureState(IEnumerable<DetectedFace> detectedFaces)
        {
            const int IntervalBeforeCheckingForStill = 500;
            const int IntervalWithoutFacesBeforeRevertingToWaitingForFaces = 3;

            if (!detectedFaces.Any())
            {
                if (this.autoCaptureState == AutoCaptureState.WaitingForStillFaces &&
                    (DateTime.Now - this.lastTimeWhenAFaceWasDetected).TotalSeconds > IntervalWithoutFacesBeforeRevertingToWaitingForFaces)
                {
                    this.autoCaptureState = AutoCaptureState.WaitingForFaces;
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.OnAutoCaptureStateChanged(this.autoCaptureState);
                    });
                }

                return;
            }

            this.lastTimeWhenAFaceWasDetected = DateTime.Now;

            switch (this.autoCaptureState)
            {
                case AutoCaptureState.WaitingForFaces:
                    // We were waiting for faces and got some... go to the "waiting for still" state
                    this.detectedFacesFromPreviousFrame = detectedFaces;
                    this.timeSinceWaitingForStill = DateTime.Now;
                    this.autoCaptureState = AutoCaptureState.WaitingForStillFaces;

                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.OnAutoCaptureStateChanged(this.autoCaptureState);
                    });

                    break;

                case AutoCaptureState.WaitingForStillFaces:
                    // See if we have been waiting for still faces long enough
                    if ((DateTime.Now - this.timeSinceWaitingForStill).TotalMilliseconds >= IntervalBeforeCheckingForStill)
                    {
                        // See if the faces are still enough
                        if (this.AreFacesStill(this.detectedFacesFromPreviousFrame, detectedFaces))
                        {
                            this.autoCaptureState = AutoCaptureState.ShowingCountdownForCapture;
                            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                this.OnAutoCaptureStateChanged(this.autoCaptureState);
                            });
                        }
                        else
                        {
                            // Faces moved too much, update the baseline and keep waiting
                            this.timeSinceWaitingForStill = DateTime.Now;
                            this.detectedFacesFromPreviousFrame = detectedFaces;
                        }
                    }
                    break;

                case AutoCaptureState.ShowingCountdownForCapture:
                    break;

                case AutoCaptureState.ShowingCapturedPhoto:
                    break;

                default:
                    break;
            }
        }

        //public async Task<ImageAnalyzer> TakeAutoCapturePhoto()
        //{
        //    var image = await CaptureFrameAsync();
        //    this.autoCaptureState = AutoCaptureState.ShowingCapturedPhoto;
        //    this.OnAutoCaptureStateChanged(this.autoCaptureState);
        //    return image;
        //}

        //public void RestartAutoCaptureCycle()
        //{
        //    this.autoCaptureState = AutoCaptureState.WaitingForFaces;
        //    this.OnAutoCaptureStateChanged(this.autoCaptureState);
        //}

        private bool AreFacesStill(IEnumerable<DetectedFace> detectedFacesFromPreviousFrame, IEnumerable<DetectedFace> detectedFacesFromCurrentFrame)
        {
            int horizontalMovementThreshold = (int)(videoProperties.Width * 0.02);
            int verticalMovementThreshold = (int)(videoProperties.Height * 0.02);

            int numStillFaces = 0;
            int totalFacesInPreviousFrame = detectedFacesFromPreviousFrame.Count();

            foreach (DetectedFace faceInPreviousFrame in detectedFacesFromPreviousFrame)
            {
                if (numStillFaces > 0 && numStillFaces >= totalFacesInPreviousFrame / 2)
                {
                    // If half or more of the faces in the previous frame are considered still we can stop. It is still enough.
                    break;
                }

                // If there is a face in the current frame that is located close enough to this one in the previous frame, we 
                // assume it is the same face and count it as a still face. 
                if (detectedFacesFromCurrentFrame.Any(f => Math.Abs((int)faceInPreviousFrame.FaceBox.X - (int)f.FaceBox.X) <= horizontalMovementThreshold &&
                                                           Math.Abs((int)faceInPreviousFrame.FaceBox.Y - (int)f.FaceBox.Y) <= verticalMovementThreshold))
                {
                    numStillFaces++;
                }
            }

            if (numStillFaces > 0 && numStillFaces >= totalFacesInPreviousFrame / 2)
            {
                // If half or more of the faces in the previous frame are considered still we consider the group as still
                return true;
            }

            return false;
        }

        //public async Task StopStreamAsync()
        //{
        //    try
        //    {
        //        if (this.frameProcessingTimer != null)
        //        {
        //            this.frameProcessingTimer.Cancel();
        //        }

        //        if (captureManager != null && captureManager.CameraStreamState != Windows.Media.Devices.CameraStreamState.Shutdown)
        //        {
        //            this.FaceTrackingVisualizationCanvas.Children.Clear();
        //            await this.captureManager.StopPreviewAsync();

        //            this.FaceTrackingVisualizationCanvas.Children.Clear();
        //            this.webCamCaptureElement.Visibility = Visibility.Collapsed;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        //await Util.GenericApiCallExceptionHandler(ex, "Error stopping the camera.");
        //    }
        //}

        //public async Task<ImageAnalyzer> CaptureFrameAsync()
        //{
        //    try
        //    {
        //        if (!(await this.frameProcessingSemaphore.WaitAsync(250)))
        //        {
        //            return null;
        //        }

        //        // Capture a frame from the preview stream
        //        var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, CameraResolutionWidth, CameraResolutionHeight);
        //        using (var currentFrame = await captureManager.GetPreviewFrameAsync(videoFrame))
        //        {
        //            using (SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap)
        //            {
        //                ImageAnalyzer imageWithFace = new ImageAnalyzer(await Util.GetPixelBytesFromSoftwareBitmapAsync(previewFrame));

        //                imageWithFace.ShowDialogOnFaceApiErrors = this.ShowDialogOnApiErrors;
        //                imageWithFace.FilterOutSmallFaces = this.FilterOutSmallFaces;
        //                imageWithFace.UpdateDecodedImageSize(this.CameraResolutionHeight, this.CameraResolutionWidth);

        //                return imageWithFace;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (this.ShowDialogOnApiErrors)
        //        {
        //            await Util.GenericApiCallExceptionHandler(ex, "Error capturing photo.");
        //        }
        //    }
        //    finally
        //    {
        //        this.frameProcessingSemaphore.Release();
        //    }

        //    return null;
        //}

        //private void OnImageCaptured(ImageAnalyzer imageWithFace)
        //{
        //    if (this.ImageCaptured != null)
        //    {
        //        this.ImageCaptured(this, imageWithFace);
        //    }
        //}

        private void OnAutoCaptureStateChanged(AutoCaptureState state)
        {
            if (this.AutoCaptureStateChanged != null)
            {
                this.AutoCaptureStateChanged(this, state);
            }
        }
    }
}