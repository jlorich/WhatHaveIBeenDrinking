using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Storage;

namespace WhatHaveIBeenDrinking
{
    public class CameraCaptureService
    {
        public CameraCaptureService()
        {

        }

        public async Task<StorageFile> Capture()
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);
            
            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            return photo;
        }
    }
}
