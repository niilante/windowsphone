﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaApp.Classes
{
    public class AppInformation
    {
        public AppInformation()
        {
            this.PickerOrAsyncDialogIsOpen = false;
            this.IsNewlyActivatedAccount = false;
            this.IsStartedAsAutoUpload = false;
            this.IsStartupModeActivate = false;
        }
        
        public bool PickerOrAsyncDialogIsOpen { get; set; }
        public bool IsNewlyActivatedAccount { get; set; }
        public bool IsStartedAsAutoUpload { get; set; }
        public bool IsStartupModeActivate { get; set; }
    }
}
