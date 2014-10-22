﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using mega;
using MegaApp.Enums;
using MegaApp.MegaApi;
using MegaApp.Resources;
using MegaApp.Services;

namespace MegaApp.Models
{
    public class TransferObjectModel : BaseSdkViewModel, MTransferListenerInterface
    {
        public TransferObjectModel(MegaSDK megaSdk, NodeViewModel selectedNode, TransferType transferType, string filePath) 
            :base(megaSdk)
        {
            switch (transferType)
            {
                case TransferType.Download:
                    {
                        DisplayName = selectedNode.Name;
                        break;
                    }
                case TransferType.Upload:
                    {
                        DisplayName = Path.GetFileName(filePath);
                        break;
                    }
            }
            Type = transferType;
            FilePath = filePath;
            Status = TransferStatus.NotStarted;
            SelectedNode = selectedNode;
            CancelButtonState = false;
            AutoLoadImageOnFinish = false;
            CancelTransferCommand = new DelegateCommand(CancelTransfer);
            SetThumbnail();
        }

        #region Commands

        public ICommand CancelTransferCommand { get; set; }

        #endregion

        #region Methods

        public void StartTransfer()
        {
            switch (Type)
            {
                case TransferType.Download:
                {
                    this.MegaSdk.startDownload(SelectedNode.GetMegaNode(), FilePath, this);
                    break;
                }
                case TransferType.Upload:
                {
                    MegaSdk.startUpload(FilePath, SelectedNode.GetMegaNode(), this);
                    break; 
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void CancelTransfer(object p = null)
        {
            if (!IsBusy) return;
            Status = TransferStatus.Canceling;
            MegaSdk.cancelTransfer(Transfer);
        }

        private void SetThumbnail()
        {
            switch (Type)
            {
                case TransferType.Download:
                    {
                        Thumbnail = ImageService.GetDefaultFileImage(SelectedNode.Name);
                        if (FileService.FileExists(SelectedNode.ThumbnailPath))
                        {
                            Thumbnail = new BitmapImage(new Uri(SelectedNode.ThumbnailPath)); ;
                        }
                        break;
                    }
                case TransferType.Upload:
                    {
                        if (ImageService.IsImage(FilePath))
                            Thumbnail = new BitmapImage(new Uri(FilePath));
                        else
                            Thumbnail = ImageService.GetDefaultFileImage(FilePath);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        #endregion

        #region Properties

        public string DisplayName { get; set; }
        public string FilePath { get; private set; }
        public TransferType Type { get; set; }
        public NodeViewModel SelectedNode { get; private set; }
        public MTransfer Transfer { get; private set; }
        public BitmapImage Thumbnail { get; private set; }
        public bool AutoLoadImageOnFinish { get; set; }

        private bool _cancelButtonState;
        public bool CancelButtonState
        {
            get { return _cancelButtonState; }
            set
            {
                _cancelButtonState = value;
                OnPropertyChanged("CancelButtonState");
            }
        }


        private TransferStatus _transferStatus;
        public TransferStatus Status
        {
            get { return _transferStatus; }
            set
            {
                _transferStatus = value;
                OnPropertyChanged("Status");
            }
        }

        private ulong _totalBytes;
        public ulong TotalBytes
        {
            get { return _totalBytes; }
            set
            {
                _totalBytes = value;
                OnPropertyChanged("TotalBytes");
            }
        }

        private ulong _transferedBytes;
        public ulong TransferedBytes
        {
            get { return _transferedBytes; }
            set
            {
                _transferedBytes = value;
                OnPropertyChanged("TransferedBytes");
            }
        }

        #endregion

        #region MTransferListenerInterface

        //Will be called only for transfers started by startStreaming
        //Return true to continue getting data, false to stop the streaming
        public bool onTransferData(MegaSDK api, MTransfer transfer, byte[] data)
        {
            return false;
        }

        public void onTransferFinish(MegaSDK api, MTransfer transfer, MError e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                TotalBytes = transfer.getTotalBytes();
                TransferedBytes = transfer.getTransferredBytes();
                IsBusy = false;
                CancelButtonState = false;
            });

            switch (e.getErrorCode())
            {
                case MErrorType.API_OK:
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>Status = TransferStatus.Finished);

                    if (AutoLoadImageOnFinish)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() => SelectedNode.LoadImage(SelectedNode.ImagePath));
                    }
                    break;
                }
                case MErrorType.API_EINCOMPLETE:
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => Status = TransferStatus.Canceled);
                    break;
                }
                default:
                {
                    Status = TransferStatus.Error;
                    switch (Type)
                    {
                        case TransferType.Download:
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                                MessageBox.Show(String.Format(AppMessages.DownloadNodeFailed, e.getErrorString()),
                                    AppMessages.DownloadNodeFailed_Title, MessageBoxButton.OK));
                            break;
                        }
                    case TransferType.Upload:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            }


            if (e.getErrorCode() == MErrorType.API_OK)
            {
                
            }
            
            else if (e.getErrorCode() != MErrorType.API_EINCOMPLETE)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    MessageBox.Show(String.Format(AppMessages.DownloadNodeFailed, e.getErrorString()),
                    AppMessages.DownloadNodeFailed_Title, MessageBoxButton.OK));
            }
        }

        public void onTransferStart(MegaSDK api, MTransfer transfer)
        {
            Transfer = transfer;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                Status = TransferStatus.Connecting;
                CancelButtonState = true;
                IsBusy = true;
                TotalBytes = transfer.getTotalBytes();
                TransferedBytes = transfer.getTransferredBytes();
            });
        }

        public void onTransferTemporaryError(MegaSDK api, MTransfer transfer, MError e)
        {
            // Do nothing
        }

        public void onTransferUpdate(MegaSDK api, MTransfer transfer)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                TotalBytes = transfer.getTotalBytes();
                TransferedBytes = transfer.getTransferredBytes();

                if (TransferedBytes > 0)
                {
                    switch (Type)
                    {
                        case TransferType.Download:
                            Status = TransferStatus.Downloading;
                            break;
                        case TransferType.Upload:
                            Status = TransferStatus.Uploading;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        #endregion
    }
}