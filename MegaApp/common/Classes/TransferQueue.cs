﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using mega;
using MegaApp.Models;
using MegaApp.Services;

namespace MegaApp.Classes
{
    public class TransferQueue
    {
        public TransferQueue()
        {
            Uploads = new ObservableCollection<TransferObjectModel>();
            Downloads = new ObservableCollection<TransferObjectModel>();
            Completed = new ObservableCollection<TransferObjectModel>();

            Uploads.CollectionChanged += OnCollectionChanged;
            Downloads.CollectionChanged += OnCollectionChanged;
            Completed.CollectionChanged += OnCollectionChanged;
        }

        /// <summary>
        /// Add a transfer to the Transfer Queue.
        /// </summary>
        /// <param name="transferObjectModel">Transfer to add</param>
        public void Add(TransferObjectModel transferObjectModel)
        {
            // Folder transfers are not included into the transfers list.
            if (transferObjectModel.IsFolderTransfer) return;

            switch (transferObjectModel.Type)
            {
                case MTransferType.TYPE_DOWNLOAD:
                    Sort(this.Downloads, transferObjectModel);
                    break;
                case MTransferType.TYPE_UPLOAD:
                    Sort(this.Uploads, transferObjectModel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Remove a transfer to the Transfer Queue.
        /// </summary>
        /// <param name="transferObjectModel">Transfer to remove</param>
        public void Remove(TransferObjectModel transferObjectModel)
        {
            if (transferObjectModel.TransferState == MTransferState.STATE_COMPLETED)
            {
                this.Completed.Remove(transferObjectModel);
                return;
            }

            switch (transferObjectModel.Type)
            {
                case MTransferType.TYPE_DOWNLOAD:
                    this.Downloads.Remove(transferObjectModel);
                    break;
                case MTransferType.TYPE_UPLOAD:
                    this.Uploads.Remove(transferObjectModel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Select and return all transfers in the queue.
        /// </summary>
        /// <returns>Download and upload transfers combined in one list.</returns>
        public IList<TransferObjectModel> SelectAll()
        {
            try
            {
                // Use temporal variables to avoid ArgumentException if
                // any new transfer is added during this operation (Possible bug #7676)
                var downloads = this.Downloads.ToList();
                var uploads = this.Uploads.ToList();

                var result = new List<TransferObjectModel>(downloads.Count + uploads.Count);
                result.AddRange(downloads);
                result.AddRange(uploads);
                return result;
            }
            catch (Exception e)
            {
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error getting the complete transfers list in queue.", e);
                return null;
            }
        }

        /// <summary>
        /// Clear the complete queue
        /// </summary>
        public void Clear()
        {
            this.Downloads.Clear();
            this.Uploads.Clear();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                        ((TransferObjectModel)item).PropertyChanged += OnStatusPropertyChanged;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                        ((TransferObjectModel)item).PropertyChanged -= OnStatusPropertyChanged;
                    break;
            }
        }

        private void OnStatusPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("TransferState")) return;

            var transferObjectModel = sender as TransferObjectModel;
            if (transferObjectModel == null) return;
            Sort(transferObjectModel.Type == MTransferType.TYPE_DOWNLOAD ? this.Downloads : this.Uploads, transferObjectModel);
        }

        public static void Sort(ObservableCollection<TransferObjectModel> transferList,
            TransferObjectModel transferObject)
        {
            if (transferList == null || transferObject == null) return;

            try
            {
                var existing = (transferObject.Transfer != null) ?
                    TransfersService.SearchTransfer(transferList, transferObject.Transfer) :
                    transferList.FirstOrDefault(t => (t.TransferPath != null && t.TransferPath.Equals(transferObject.TransferPath)));

                bool handled = false;
                bool move = existing != null;
                var index = transferList.IndexOf(existing);
                var count = transferList.Count - 1;

                for (var i = 0; i <= count; i++)
                {
                    if ((int)transferObject.TransferPriority > (int)transferList[i].TransferPriority) continue;

                    if (move)
                    {
                        if (index != i)
                        {
                            transferList.RemoveAt(index);
                            transferList.Insert(i, transferObject);
                        }
                    }
                    else
                    {
                        transferList.Insert(i, transferObject);
                    }
                    handled = true;
                    break;
                }

                if (handled) return;

                if (move)
                {
                    if (index != count)
                    {
                        transferList.RemoveAt(index);
                        transferList.Insert(count, transferObject);
                    }
                }
                else
                {
                    transferList.Add(transferObject);
                }
            }
            catch (Exception e)
            {
                var message = (transferObject.DisplayName == null) ? "Error sorting transfer" :
                    string.Format("Error sorting transfer. File: '{0}'", transferObject.DisplayName);
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, message, e);
                return;
            }
        }

        public ObservableCollection<TransferObjectModel> Uploads { get; private set; }

        public ObservableCollection<TransferObjectModel> Downloads { get; private set; }

        public ObservableCollection<TransferObjectModel> Completed { get; private set; }
    }
}
