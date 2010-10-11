﻿// ImageListView - A listview control for image files
// Copyright (C) 2009 Ozgur Ozcitak
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ozgur Ozcitak (ozcitak@yahoo.com)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Resources;
using System.Reflection;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents a listview control for image files.
    /// </summary>
    [ToolboxBitmap(typeof(ImageListView))]
    [Description("Represents a listview control for image files.")]
    [DefaultEvent("ItemClick")]
    [DefaultProperty("Items")]
    [Designer(typeof(ImageListViewDesigner))]
    [Docking(DockingBehavior.Ask)]
    public partial class ImageListView : Control, IComponent
    {
        #region Constants
        /// <summary>
        /// Default width of column headers in pixels.
        /// </summary>
        internal const int DefaultColumnWidth = 100;
        /// <summary>
        /// Selection tolerance for column separators.
        /// </summary>
        internal const int SeparatorSize = 12;
        /// <summary>
        /// Selection tolerance for left-pane border.
        /// </summary>
        internal const int PaneBorderSize = 4;
        /// <summary>
        /// Creates a control with a border.
        /// </summary>
        private const int WS_BORDER = 0x00800000;
        /// <summary>
        /// Specifies that the control has a border with a sunken edge.
        /// </summary>
        private const int WS_EX_CLIENTEDGE = 0x00000200;
        #endregion

        #region Member Variables
        // Set when properties change
        private bool mDefaultImageChanged = false;
        private bool mErrorImageChanged = false;
        private bool mRatingImageChanged = false;
        private bool mEmptyRatingImageChanged = false;
        // Properties
        private BorderStyle mBorderStyle;
        private CacheMode mCacheMode;
        private int mCacheLimitAsItemCount;
        private long mCacheLimitAsMemory;
        private ImageListViewColor mColors;
        private ImageListViewColumnHeaderCollection mColumns;
        private Image mDefaultImage;
        private Image mErrorImage;
        private Image mRatingImage;
        private Image mEmptyRatingImage;
        private Font mHeaderFont;
        private bool mIntegralScroll;
        private ImageListViewItemCollection mItems;
        private int mPaneWidth;
        private bool mRetryOnError;
        internal ImageListViewSelectedItemCollection mSelectedItems;
        internal ImageListViewCheckedItemCollection mCheckedItems;
        private int mSortColumn;
        private SortOrder mSortOrder;
        private bool mShowFileIcons;
        private bool mShowCheckBoxes;
        private ContentAlignment mIconAlignment;
        private Size mIconPadding;
        private ContentAlignment mCheckBoxAlignment;
        private Size mCheckBoxPadding;
        private Size mThumbnailSize;
        private UseEmbeddedThumbnails mUseEmbeddedThumbnails;
        private UseWIC mUseWIC;
        private View mView;
        private Point mViewOffset;

        // Renderer variables
        internal ImageListViewRenderer mRenderer;
        private bool controlSuspended;
        private int rendererSuspendCount;
        private bool rendererNeedsPaint;
        private Timer lazyRefreshTimer;

        // Layout variables
        internal HScrollBar hScrollBar;
        internal VScrollBar vScrollBar;
        internal ImageListViewLayoutManager layoutManager;
        private bool disposed;

        // Interaction variables
        internal ImageListViewNavigationManager navigationManager;

        // Cache threads
        internal ImageListViewCacheThumbnail thumbnailCache;
        internal ImageListViewCacheShellInfo shellInfoCache;
        internal ImageListViewCacheMetadata itemCacheManager;

        // Resource manager
        private ResourceManager resources;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets whether thumbnail images are automatically rotated.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether thumbnail images are automatically rotated."), DefaultValue(true)]
        public bool AutoRotateThumbnails { get; set; }
        /// <summary>
        /// Gets or sets whether column headers respond to mouse clicks.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether column headers respond to mouse clicks."), DefaultValue(true)]
        public bool AllowColumnClick { get; set; }
        /// <summary>
        /// Gets or sets whether column headers can be resized with the mouse.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether column headers can be resized with the mouse."), DefaultValue(true)]
        public bool AllowColumnResize { get; set; }
        /// <summary>
        /// Gets or sets whether the user can drag items for drag-and-drop operations.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether the user can drag items for drag-and-drop operations."), DefaultValue(false)]
        public bool AllowDrag { get; set; }
        /// <summary>
        /// Gets or sets whether duplicate items (image files pointing to the same path 
        /// on the file system) are allowed.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether duplicate items (image files pointing to the same path on the file system) are allowed."), DefaultValue(false)]
        public bool AllowDuplicateFileNames { get; set; }
        /// <summary>
        /// Gets or sets whether the left-pane can be resized with the mouse.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether the left-pane can be resized with the mouse."), DefaultValue(true)]
        public bool AllowPaneResize { get; set; }
        /// <summary>
        /// Gets or sets the background color of the control.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the background color of the control."), DefaultValue(typeof(Color), "Window")]
        public override Color BackColor { get { return mColors.ControlBackColor; } set { mColors.ControlBackColor = value; Refresh(); } }
        /// <summary>
        /// Gets or sets the border style of the control.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the border style of the control."), DefaultValue(typeof(BorderStyle), "Fixed3D")]
        public BorderStyle BorderStyle { get { return mBorderStyle; } set { mBorderStyle = value; UpdateStyles(); } }
        /// <summary>
        /// Gets or sets the cache mode. Setting the the CacheMode to Continuous disables the CacheLimit.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the cache mode."), DefaultValue(typeof(CacheMode), "OnDemand"), RefreshProperties(RefreshProperties.All)]
        public CacheMode CacheMode
        {
            get
            {
                return mCacheMode;
            }
            set
            {
                if (mCacheMode != value)
                {
                    mCacheMode = value;

                    if (thumbnailCache != null)
                        thumbnailCache.CacheMode = mCacheMode;

                    if (mCacheMode == CacheMode.Continuous)
                    {
                        mCacheLimitAsItemCount = 0;
                        mCacheLimitAsMemory = 0;
                        if (thumbnailCache != null)
                        {
                            thumbnailCache.CacheLimitAsItemCount = 0;
                            thumbnailCache.CacheLimitAsMemory = 0;
                        }
                        // Rebuild the cache
                        ClearThumbnailCache();
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets the cache limit as either the count of thumbnail images or the memory allocated for cache (e.g. 10MB).
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the cache limit as either the count of thumbnail images or the memory allocated for cache (e.g. 10MB)."), DefaultValue("20MB"), RefreshProperties(RefreshProperties.All)]
        public string CacheLimit
        {
            get
            {
                if (mCacheLimitAsMemory != 0)
                    return (mCacheLimitAsMemory / 1024 / 1024).ToString() + "MB";
                else
                    return mCacheLimitAsItemCount.ToString();
            }
            set
            {
                string slimit = value;
                int limit = 0;
                mCacheMode = CacheMode.OnDemand;
                if ((slimit.EndsWith("MB", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(slimit.Substring(0, slimit.Length - 2).Trim(), out limit)) ||
                    (slimit.EndsWith("MiB", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(slimit.Substring(0, slimit.Length - 3).Trim(), out limit)))
                {
                    mCacheLimitAsItemCount = 0;
                    mCacheLimitAsMemory = limit * 1024 * 1024;
                    if (thumbnailCache != null)
                    {
                        thumbnailCache.CacheLimitAsItemCount = 0;
                        thumbnailCache.CacheLimitAsMemory = mCacheLimitAsMemory;
                    }
                }
                else if (int.TryParse(slimit, out limit))
                {
                    mCacheLimitAsMemory = 0;
                    mCacheLimitAsItemCount = limit;
                    if (thumbnailCache != null)
                    {
                        thumbnailCache.CacheLimitAsMemory = 0;
                        thumbnailCache.CacheLimitAsItemCount = mCacheLimitAsItemCount;
                    }
                }
                else
                    throw new ArgumentException("Cache limit must be specified as either the count of thumbnail images or the memory allocated for cache (eg 10MB)", "value");
            }
        }
        /// <summary>
        /// Gets or sets the color palette of the ImageListView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the color palette of the ImageListView.")]
        public ImageListViewColor Colors
        {
            get
            {
                if (mColors == null)
                    mColors = ImageListViewColor.Default;

                return mColors;
            }
            set
            {
                mColors = value;
                Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the collection of columns of the image list view.
        /// </summary>
        [Category("Appearance"), Description("Gets the collection of columns of the image list view.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageListViewColumnHeaderCollection Columns { get { return mColumns; } internal set { mColumns = value; Refresh(); } }
        /// <summary>
        /// Gets or sets the placeholder image.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the placeholder image.")]
        public Image DefaultImage
        {
            get
            {
                if (mDefaultImage == null)
                    return resources.GetObject("DefaultImage") as Image;
                else
                    return mDefaultImage;
            }
            set
            {
                mDefaultImageChanged = true;
                mDefaultImage = value;
                Refresh();
            }
        }
        /// <summary>
        /// Gets the rectangle that represents the display area of the control.
        /// </summary>
        [Category("Appearance"), Browsable(false), Description("Gets the rectangle that represents the display area of the control.")]
        public override Rectangle DisplayRectangle
        {
            get
            {
                if (layoutManager == null)
                    return base.DisplayRectangle;
                else
                    return layoutManager.ClientArea;
            }
        }
        /// <summary>
        /// Gets or sets the error image.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the error image.")]
        public Image ErrorImage
        {
            get
            {
                if (mErrorImage == null)
                    return resources.GetObject("ErrorImage") as Image;
                else
                    return mErrorImage;
            }
            set
            {
                mErrorImageChanged = true;
                mErrorImage = value;
                Refresh();
            }
        }
        /// <summary>
        /// Gets or sets the font of the column headers.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the font of the column headers.")]
        public Font HeaderFont
        {
            get
            {
                if (mHeaderFont != null)
                    return mHeaderFont;
                else if (Font != null)
                    return Font;
                else
                    return Control.DefaultFont;
            }
            set
            {
                mHeaderFont = value;
                Refresh();
            }
        }
        /// <summary>
        /// Gets or sets whether scrollbars scroll by an amount which is a multiple of item height.
        /// </summary>
        [Browsable(true), Category("Behavior"), Description("Gets or sets whether scrollbars scroll by an amount which is a multiple of item height."), DefaultValue(false)]
        public bool IntegralScroll
        {
            get
            {
                return mIntegralScroll;
            }
            set
            {
                if (mIntegralScroll != value)
                {
                    mIntegralScroll = value;
                    Refresh();
                }
            }
        }
        /// <summary>
        /// Gets the collection of items contained in the image list view.
        /// </summary>
        [Category("Behavior"), Description("Gets the collection of items contained in the image list view.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageListViewItemCollection Items { get { return mItems; } internal set { mItems = value; Refresh(); } }
        /// <summary>
        /// Gets or sets whether multiple items can be selected.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether multiple items can be selected."), DefaultValue(true)]
        public bool MultiSelect { get; set; }
        /// <summary>
        /// Gets or sets the width of the left pane.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the width of the left pane."), DefaultValue(240)]
        public int PaneWidth
        {
            get
            {
                return mPaneWidth;
            }
            set
            {
                if (mPaneWidth != value)
                {
                    if (mPaneWidth < 2) mPaneWidth = 2;
                    mPaneWidth = value;
                    Refresh();
                }
            }
        }
        /// <summary>
        /// Gets or sets the rating image.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the rating image.")]
        public Image RatingImage
        {
            get
            {
                if (mRatingImage == null)
                    return resources.GetObject("RatingImage") as Image;
                else
                    return mRatingImage;
            }
            set
            {
                mRatingImageChanged = true;
                mRatingImage = value;
                Refresh();
            }
        }
        /// <summary>
        /// Gets or sets the empty rating image.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the empty rating image.")]
        public Image EmptyRatingImage
        {
            get
            {
                if (mEmptyRatingImage == null)
                    return resources.GetObject("EmptyRatingImage") as Image;
                else
                    return mEmptyRatingImage;
            }
            set
            {
                mEmptyRatingImageChanged = true;
                mEmptyRatingImage = value;
                Refresh();
            }
        }
        /// <summary>
        /// Gets or sets whether the control will retry loading thumbnails on an error.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether the control will retry loading thumbnails on an error."), DefaultValue(true)]
        public bool RetryOnError
        {
            get
            {
                return mRetryOnError;
            }
            set
            {
                mRetryOnError = value;
                if (thumbnailCache != null)
                    thumbnailCache.RetryOnError = mRetryOnError;
                if (shellInfoCache != null)
                    shellInfoCache.RetryOnError = mRetryOnError;
                if (itemCacheManager != null)
                    itemCacheManager.RetryOnError = mRetryOnError;
            }
        }
        /// <summary>
        /// Gets or sets whether the scrollbars should be shown.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets whether the scrollbars should be shown."), DefaultValue(true)]
        public bool ScrollBars { get; set; }
        /// <summary>
        /// Gets the collection of selected items contained in the image list view.
        /// </summary>
        [Browsable(false), Category("Behavior"), Description("Gets the collection of selected items contained in the image list view.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageListViewSelectedItemCollection SelectedItems { get { return mSelectedItems; } }
        /// <summary>
        /// Gets the collection of checked items contained in the image list view.
        /// </summary>
        [Browsable(false), Category("Behavior"), Description("Gets the collection of checked items contained in the image list view.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ImageListViewCheckedItemCollection CheckedItems { get { return mCheckedItems; } }
        /// <summary>
        /// Gets or sets whether shell icons are displayed for non-image files.
        /// </summary>
        [Browsable(false), Category("Behavior"), Description("Gets or sets whether shell icons are displayed for non-image files."), DefaultValue(true)]
        public bool ShellIconFallback { get; set; }
        /// <summary>
        /// Gets or sets whether to display the file icons.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets whether to display the file icons."), DefaultValue(false)]
        public bool ShowFileIcons
        {
            get { return mShowFileIcons; }
            set { mShowFileIcons = value; Refresh(); }
        }
        /// <summary>
        /// Gets or sets whether to display the item checkboxes.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets whether to display the item checkboxes."), DefaultValue(false)]
        public bool ShowCheckBoxes
        {
            get { return mShowCheckBoxes; }
            set { mShowCheckBoxes = value; Refresh(); }
        }
        /// <summary>
        /// Gets or sets alignment of file icons.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets alignment of file icons."), DefaultValue(ContentAlignment.TopRight)]
        public ContentAlignment IconAlignment
        {
            get { return mIconAlignment; }
            set { mIconAlignment = value; Refresh(); }
        }
        /// <summary>
        /// Gets or sets file icon padding.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets file icon padding."), DefaultValue(typeof(Size), "2,2")]
        public Size IconPadding
        {
            get { return mIconPadding; }
            set { mIconPadding = value; Refresh(); }
        }
        /// <summary>
        /// Gets or sets alignment of item checkboxes.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets alignment of item checkboxes."), DefaultValue(ContentAlignment.BottomRight)]
        public ContentAlignment CheckBoxAlignment
        {
            get { return mCheckBoxAlignment; }
            set { mCheckBoxAlignment = value; Refresh(); }
        }
        /// <summary>
        /// Gets or sets item checkbox padding.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets item checkbox padding."), DefaultValue(typeof(Size), "2,2")]
        public Size CheckBoxPadding
        {
            get { return mCheckBoxPadding; }
            set { mCheckBoxPadding = value; Refresh(); }
        }
        /// <summary>
        /// Gets or sets the index of the sort column.
        /// </summary>
        [Category("Appearance"), DefaultValue(0), Description("Gets or sets the index of the sort column.")]
        public int SortColumn
        {
            get
            {
                return mSortColumn;
            }
            set
            {
                if (value != mSortColumn)
                {
                    mSortColumn = value;
                    Sort();
                }
            }
        }
        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        [Category("Appearance"), DefaultValue(typeof(SortOrder), "None"), Description("Gets or sets the sort order.")]
        public SortOrder SortOrder
        {
            get
            {
                return mSortOrder;
            }
            set
            {
                if (value != mSortOrder)
                {
                    mSortOrder = value;
                    Sort();
                }
            }
        }
        /// <summary>
        /// This property is not relevant for this class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false), Bindable(false), DefaultValue(null), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text { get; set; }
        /// <summary>
        /// Gets or sets the size of image thumbnails.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the size of image thumbnails."), DefaultValue(typeof(Size), "96,96")]
        public Size ThumbnailSize
        {
            get
            {
                return mThumbnailSize;
            }
            set
            {
                if (mThumbnailSize != value)
                {
                    mThumbnailSize = value;
                    thumbnailCache.CurrentThumbnailSize = mThumbnailSize;
                    thumbnailCache.Rebuild();
                    Refresh();
                }
            }
        }
        /// <summary>
        /// Gets or sets the embedded thumbnails extraction behavior.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the embedded thumbnails extraction behavior."), DefaultValue(typeof(UseEmbeddedThumbnails), "Auto")]
        public UseEmbeddedThumbnails UseEmbeddedThumbnails
        {
            get
            {
                return mUseEmbeddedThumbnails;
            }
            set
            {
                if (mUseEmbeddedThumbnails != value)
                {
                    mUseEmbeddedThumbnails = value;
                    Refresh();
                }
            }
        }
        /// <summary>
        /// Gets or sets whether Windows Imaging Compomnent will be used.
        /// </summary>
        [Browsable(false), Category("Behavior"), Description("Gets or sets whether Windows Imaging Compomnent will be used."), DefaultValue(typeof(UseWIC), "Auto")]
        public UseWIC UseWIC
        {
            get
            {
                return mUseWIC;
            }
            set
            {
                if (mUseWIC != value)
                {
                    mUseWIC = value;
                    Refresh();
                }
            }
        }
        /// <summary>
        /// Gets or sets the view mode of the image list view.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the view mode of the image list view."), DefaultValue(typeof(View), "Thumbnails")]
        public View View
        {
            get
            {
                return mView;
            }
            set
            {
                SuspendPaint();
                int current = layoutManager.FirstVisible;
                mView = value;
                layoutManager.Update();
                EnsureVisible(current);
                Refresh();
                ResumePaint();
            }
        }
        /// <summary>
        /// Gets or sets the scroll offset.
        /// </summary>
        internal Point ViewOffset { get { return mViewOffset; } set { mViewOffset = value; } }
        /// <summary>
        /// Gets the scroll orientation.
        /// </summary>
        internal ScrollOrientation ScrollOrientation { get { return (mView == View.Gallery ? ScrollOrientation.HorizontalScroll : ScrollOrientation.VerticalScroll); } }
        /// <summary>
        /// Gets the required creation parameters when the control handle is created.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p = base.CreateParams;
                p.Style &= ~WS_BORDER;
                p.ExStyle &= ~WS_EX_CLIENTEDGE;
                if (mBorderStyle == BorderStyle.Fixed3D)
                    p.ExStyle |= WS_EX_CLIENTEDGE;
                else if (mBorderStyle == BorderStyle.FixedSingle)
                    p.Style |= WS_BORDER;
                return p;
            }
        }
        #endregion

        #region Custom Property Serializers
        /// <summary>
        /// Determines if the header font should be serialized.
        /// </summary>
        /// <returns>true if the designer should serialize 
        /// the property; otherwise false.</returns>
        public bool ShouldSerializeHeaderFont()
        {
            using (Font font = new Font("Microsoft Sans Serif", 8.25f))
            {
                return !mHeaderFont.Equals(font);
            }
        }
        /// <summary>
        /// Resets the header font to its default value.
        /// </summary>
        public void ResetHeaderFont()
        {
            HeaderFont = new Font("Microsoft Sans Serif", 8.25f);
        }

        /// <summary>
        /// Determines if the colors should be serialized.
        /// </summary>
        /// <returns>true if the designer should serialize 
        /// the property; otherwise false.</returns>
        public bool ShouldSerializeColors()
        {
            ImageListViewColor defaultColors = ImageListViewColor.Default;
            return !mColors.Equals(defaultColors);
        }
        /// <summary>
        /// Resets the colors to their default value.
        /// </summary>
        public void ResetColors()
        {
            Colors = ImageListViewColor.Default;
        }

        /// <summary>
        /// Determines if the default image should be serialized.
        /// </summary>
        /// <returns>true if the designer should serialize 
        /// the property; otherwise false.</returns>
        public bool ShouldSerializeDefaultImage()
        {
            return mDefaultImageChanged;
        }
        /// <summary>
        /// Resets the default image to its default value.
        /// </summary>
        public void ResetDefaultImage()
        {
            DefaultImage = resources.GetObject("DefaultImage") as Image;
            mDefaultImageChanged = false;
        }

        /// <summary>
        /// Determines if the error image should be serialized.
        /// </summary>
        /// <returns>true if the designer should serialize 
        /// the property; otherwise false.</returns>
        public bool ShouldSerializeErrorImage()
        {
            return mErrorImageChanged;
        }
        /// <summary>
        /// Resets the error image to its default value.
        /// </summary>
        public void ResetErrorImage()
        {
            ErrorImage = resources.GetObject("ErrorImage") as Image;
            mErrorImageChanged = false;
        }

        /// <summary>
        /// Determines if the rating image should be serialized.
        /// </summary>
        /// <returns>true if the designer should serialize 
        /// the property; otherwise false.</returns>
        public bool ShouldSerializeRatingImage()
        {
            return mRatingImageChanged;
        }
        /// <summary>
        /// Resets the rating image to its default value.
        /// </summary>
        public void ResetRatingImage()
        {
            RatingImage = resources.GetObject("RatingImage") as Image;
            mRatingImageChanged = false;
        }

        /// <summary>
        /// Determines if the empty rating image should be serialized.
        /// </summary>
        /// <returns>true if the designer should serialize 
        /// the property; otherwise false.</returns>
        public bool ShouldSerializeEmptyRatingImage()
        {
            return mEmptyRatingImageChanged;
        }
        /// <summary>
        /// Resets the empty rating image to its default value.
        /// </summary>
        public void ResetEmptyRatingImage()
        {
            EmptyRatingImage = resources.GetObject("EmptyRatingImage") as Image;
            mEmptyRatingImageChanged = false;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ImageListView class.
        /// </summary>
        public ImageListView()
        {
            // Renderer parameters
            controlSuspended = false;
            rendererSuspendCount = 0;
            rendererNeedsPaint = true;

            mColors = ImageListViewColor.Default;
            SetRenderer(new ImageListViewRenderer());

            // Property defaults
            AutoRotateThumbnails = true;
            AllowColumnClick = true;
            AllowColumnResize = true;
            AllowDrag = false;
            AllowDuplicateFileNames = false;
            AllowPaneResize = true;
            mBorderStyle = BorderStyle.Fixed3D;
            mCacheMode = CacheMode.OnDemand;
            mCacheLimitAsItemCount = 0;
            mCacheLimitAsMemory = 20 * 1024 * 1024;
            mColumns = new ImageListViewColumnHeaderCollection(this);
            resources = new ResourceManager("Manina.Windows.Forms.ImageListViewResources", GetType().Assembly);
            mDefaultImage = resources.GetObject("DefaultImage") as Image;
            mErrorImage = resources.GetObject("ErrorImage") as Image;
            mRatingImage = resources.GetObject("RatingImage") as Image;
            mEmptyRatingImage = resources.GetObject("EmptyRatingImage") as Image;
            HeaderFont = new Font("Microsoft Sans Serif", 8.25f);
            mIntegralScroll = false;
            mItems = new ImageListViewItemCollection(this);
            MultiSelect = true;
            mPaneWidth = 240;
            mRetryOnError = true;
            mSelectedItems = new ImageListViewSelectedItemCollection(this);
            mCheckedItems = new ImageListViewCheckedItemCollection(this);
            mSortColumn = 0;
            mSortOrder = SortOrder.None;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque |
                ControlStyles.Selectable | ControlStyles.UserMouse, true);
            ScrollBars = true;
            ShellIconFallback = true;
            Size = new Size(120, 100);
            mShowCheckBoxes = false;
            mCheckBoxAlignment = ContentAlignment.BottomRight;
            mCheckBoxPadding = new Size(2, 2);
            mShowFileIcons = false;
            mIconAlignment = ContentAlignment.TopRight;
            mIconPadding = new Size(2, 2);
            Text = string.Empty;
            mThumbnailSize = new Size(96, 96);
            mUseEmbeddedThumbnails = UseEmbeddedThumbnails.Auto;
            mUseWIC = UseWIC.Auto;
            mView = View.Thumbnails;
            mViewOffset = new Point(0, 0);

            // Child controls
            hScrollBar = new HScrollBar();
            vScrollBar = new VScrollBar();
            hScrollBar.Visible = false;
            vScrollBar.Visible = false;
            hScrollBar.Scroll += hScrollBar_Scroll;
            vScrollBar.Scroll += vScrollBar_Scroll;
            Controls.Add(hScrollBar);
            Controls.Add(vScrollBar);

            // Lazy refresh timer
            lazyRefreshTimer = new Timer();
            lazyRefreshTimer.Interval = ImageListViewRenderer.LazyRefreshInterval;
            lazyRefreshTimer.Enabled = false;
            lazyRefreshTimer.Tick += lazyRefreshTimer_Tick;

            // Helpers
            layoutManager = new ImageListViewLayoutManager(this);
            navigationManager = new ImageListViewNavigationManager(this);

            thumbnailCache = new ImageListViewCacheThumbnail(this);
            thumbnailCache.CurrentThumbnailSize = mThumbnailSize;
            shellInfoCache = new ImageListViewCacheShellInfo(this);
            itemCacheManager = new ImageListViewCacheMetadata(this);

            disposed = false;
        }
        #endregion

        #region Select/Check
        /// <summary>
        /// Marks all items as selected.
        /// </summary>
        public void SelectAll()
        {
            SuspendPaint();

            foreach (ImageListViewItem item in Items)
                item.mSelected = true;

            OnSelectionChangedInternal();

            Refresh();
            ResumePaint();
        }
        /// <summary>
        /// Marks all items as unselected.
        /// </summary>
        public void ClearSelection()
        {
            SuspendPaint();
            mSelectedItems.Clear();
            Refresh();
            ResumePaint();
        }
        /// <summary>
        /// Reverses the selection state of all items.
        /// </summary>
        public void InvertSelection()
        {
            SuspendPaint();

            foreach (ImageListViewItem item in Items)
                item.mSelected = !item.mSelected;

            OnSelectionChangedInternal();

            Refresh();
            ResumePaint();
        }
        /// <summary>
        /// Marks all items as checked.
        /// </summary>
        public void CheckAll()
        {
            SuspendPaint();

            foreach (ImageListViewItem item in Items)
            {
                item.mChecked = true;
                OnItemCheckBoxClickInternal(item);
            }

            Refresh();
            ResumePaint();
        }
        /// <summary>
        /// Marks all items as unchecked.
        /// </summary>
        public void UncheckAll()
        {
            SuspendPaint();
            mCheckedItems.Clear();
            Refresh();
            ResumePaint();
        }
        /// <summary>
        /// Reverses the check state of all items.
        /// </summary>
        public void InvertCheckState()
        {
            SuspendPaint();

            foreach (ImageListViewItem item in Items)
            {
                item.mChecked = !item.mChecked;
                OnItemCheckBoxClickInternal(item);
            }

            Refresh();
            ResumePaint();
        }
        #endregion

        #region Instance Methods
        /// <summary>
        /// Clears the thumbnail cache.
        /// </summary>
        public void ClearThumbnailCache()
        {
            if (thumbnailCache != null)
            {
                thumbnailCache.Clear();
                if (CacheMode == CacheMode.Continuous)
                {
                    foreach (ImageListViewItem item in mItems)
                    {
                        if (item.isVirtualItem)
                            thumbnailCache.Add(item.Guid, item.VirtualItemKey,
                                mThumbnailSize, mUseEmbeddedThumbnails, AutoRotateThumbnails,
                                (mUseWIC == UseWIC.Auto || mUseWIC == UseWIC.ThumbnailsOnly));
                        else
                            thumbnailCache.Add(item.Guid, item.FileName,
                                mThumbnailSize, mUseEmbeddedThumbnails, AutoRotateThumbnails,
                                (mUseWIC == UseWIC.Auto || mUseWIC == UseWIC.ThumbnailsOnly));
                    }
                }
                Refresh();
            }
        }
        /// <summary>
        /// Temporarily suspends the layout logic for the control.
        /// </summary>
        public new void SuspendLayout()
        {
            if (controlSuspended) return;

            controlSuspended = true;
            base.SuspendLayout();
            SuspendPaint();
        }
        /// <summary>
        /// Resumes usual layout logic.
        /// </summary>
        public new void ResumeLayout()
        {
            ResumeLayout(false);
        }
        /// <summary>
        /// Resumes usual layout logic, optionally forcing an immediate layout of pending layout requests.
        /// </summary>
        /// <param name="performLayout">true to execute pending layout requests; otherwise, false.</param>
        public new void ResumeLayout(bool performLayout)
        {
            if (!controlSuspended) return;

            controlSuspended = false;
            base.ResumeLayout(performLayout);
            if (performLayout) Refresh();
            ResumePaint();
        }
        /// <summary>
        /// Sets the renderer for this instance.
        /// </summary>
        public void SetRenderer(ImageListViewRenderer renderer)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            if (mRenderer != null)
                mRenderer.Dispose();

            mRenderer = renderer;
            mRenderer.ImageListView = this;
            if (layoutManager != null)
                layoutManager.Update(true);

            Refresh();
        }
        /// <summary>
        /// Sorts the items.
        /// </summary>
        public void Sort()
        {
            mItems.Sort();
            Refresh();
        }
        /// <summary>
        /// Determines the image list view element under the specified coordinates.
        /// </summary>
        /// <param name="pt">The client coordinates of the point to be tested.</param>
        /// <param name="hitInfo">Details of the hit test.</param>
        public void HitTest(Point pt, out HitInfo hitInfo)
        {
            if (View == View.Details && pt.Y <= mRenderer.MeasureColumnHeaderHeight())
            {
                int i = 0;
                int x = layoutManager.ColumnHeaderBounds.Left;
                ImageListViewColumnHeader colIndex = null;
                ImageListViewColumnHeader sepIndex = null;
                if (AllowColumnClick || AllowColumnResize)
                {
                    foreach (ImageListViewColumnHeader col in Columns.GetDisplayedColumns())
                    {
                        // Over a column?
                        if (pt.X >= x && pt.X < x + col.Width + SeparatorSize / 2)
                            colIndex = col;

                        // Over a colummn separator?
                        if (pt.X > x + col.Width - SeparatorSize / 2 && pt.X < x + col.Width + SeparatorSize / 2)
                            sepIndex = col;

                        if (colIndex != null) break;
                        x += col.Width;
                        i++;
                    }
                }
                hitInfo = new HitInfo(colIndex, sepIndex);
            }
            else if (View == View.Pane && pt.X <= mPaneWidth)
            {
                bool overBorder = (pt.X >= mPaneWidth - PaneBorderSize);
                hitInfo = new HitInfo(overBorder);
            }
            else
            {
                int itemIndex = -1;
                bool checkBoxHit = false;
                int subItemIndex = -1;

                // Normalize to item area coordinates
                pt.X -= layoutManager.ItemAreaBounds.Left;
                pt.Y -= layoutManager.ItemAreaBounds.Top;

                if (pt.X > 0 && pt.Y > 0)
                {
                    int col = (pt.X + mViewOffset.X) / layoutManager.ItemSizeWithMargin.Width;
                    int row = (pt.Y + mViewOffset.Y) / layoutManager.ItemSizeWithMargin.Height;

                    if (ScrollOrientation == ScrollOrientation.HorizontalScroll ||
                        (ScrollOrientation == ScrollOrientation.VerticalScroll && col <= layoutManager.Cols))
                    {
                        int index = row * layoutManager.Cols + col;
                        if (index >= 0 && index <= Items.Count - 1)
                        {
                            Rectangle bounds = layoutManager.GetItemBounds(index);
                            if (bounds.Contains(pt.X + layoutManager.ItemAreaBounds.Left, pt.Y + layoutManager.ItemAreaBounds.Top))
                                itemIndex = index;
                            if (ShowCheckBoxes)
                            {
                                Rectangle checkBoxBounds = layoutManager.GetCheckBoxBounds(index);
                                if (checkBoxBounds.Contains(pt.X + layoutManager.ItemAreaBounds.Left, pt.Y + layoutManager.ItemAreaBounds.Top))
                                    checkBoxHit = true;
                            }
                        }
                    }

                    // Calculate sub item index
                    if (itemIndex != -1 && View == View.Details)
                    {
                        int xc1 = layoutManager.ColumnHeaderBounds.Left;
                        int colIndex = 0;
                        foreach (ImageListViewColumnHeader column in mColumns.GetDisplayedColumns())
                        {
                            int xc2 = xc1 + column.Width;
                            if (pt.X >= xc1 && pt.X < xc2)
                            {
                                subItemIndex = colIndex;
                                break;
                            }
                            colIndex++;
                            xc1 = xc2;
                        }
                    }
                }

                hitInfo = new HitInfo(itemIndex, subItemIndex, checkBoxHit);
            }
        }
        /// <summary>
        /// Scrolls the image list view to ensure that the item with the specified 
        /// index is visible on the screen.
        /// </summary>
        /// <param name="itemIndex">The index of the item to make visible.</param>
        /// <returns>true if the item was made visible; otherwise false (item is already visible or the image list view is empty).</returns>
        public bool EnsureVisible(int itemIndex)
        {
            if (itemIndex == -1) return false;
            if (Items.Count == 0) return false;

            // Already visible?
            Rectangle bounds = layoutManager.ItemAreaBounds;
            Rectangle itemBounds = layoutManager.GetItemBounds(itemIndex);
            if (!bounds.Contains(itemBounds))
            {
                if (ScrollOrientation == ScrollOrientation.HorizontalScroll)
                {
                    int delta = 0;
                    if (itemBounds.Left < bounds.Left)
                        delta = bounds.Left - itemBounds.Left;
                    else
                    {
                        int topItemIndex = itemIndex - (layoutManager.Cols - 1) * layoutManager.Rows;
                        if (topItemIndex < 0) topItemIndex = 0;
                        delta = bounds.Left - layoutManager.GetItemBounds(topItemIndex).Left;
                    }
                    int newXOffset = mViewOffset.X - delta;
                    if (newXOffset > hScrollBar.Maximum - hScrollBar.LargeChange + 1)
                        newXOffset = hScrollBar.Maximum - hScrollBar.LargeChange + 1;
                    if (newXOffset < hScrollBar.Minimum)
                        newXOffset = hScrollBar.Minimum;
                    mViewOffset.X = newXOffset;
                    mViewOffset.Y = 0;
                    hScrollBar.Value = newXOffset;
                    vScrollBar.Value = 0;
                }
                else
                {
                    int delta = 0;
                    if (itemBounds.Top < bounds.Top)
                        delta = bounds.Top - itemBounds.Top;
                    else
                    {
                        int topItemIndex = itemIndex - (layoutManager.Rows - 1) * layoutManager.Cols;
                        if (topItemIndex < 0) topItemIndex = 0;
                        delta = bounds.Top - layoutManager.GetItemBounds(topItemIndex).Top;
                    }
                    int newYOffset = mViewOffset.Y - delta;
                    if (newYOffset > vScrollBar.Maximum - vScrollBar.LargeChange + 1)
                        newYOffset = vScrollBar.Maximum - vScrollBar.LargeChange + 1;
                    if (newYOffset < vScrollBar.Minimum)
                        newYOffset = vScrollBar.Minimum;
                    mViewOffset.X = 0;
                    mViewOffset.Y = newYOffset;
                    hScrollBar.Value = 0;
                    vScrollBar.Value = newYOffset;
                }
                Refresh();
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Determines whether the specified item is visible on the screen.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>An ItemVisibility value.</returns>
        public ItemVisibility IsItemVisible(ImageListViewItem item)
        {
            return IsItemVisible(item.Index);
        }
        #endregion

        #region Rendering Methods
        /// <summary>
        /// Refreshes the control.
        /// </summary>
        /// <param name="force">Forces a refresh even if the renderer is suspended.</param>
        /// <param name="lazy">Refreshes the control only if a set amount of time
        /// has passed since the last refresh.</param>
        internal void Refresh(bool force, bool lazy)
        {
            if (force)
                base.Refresh();
            else if (lazy && CanPaint())
            {
                if (mRenderer.LazyRefreshIntervalExceeded)
                    base.Refresh();
                else
                {
                    rendererNeedsPaint = true;
                    if (!lazyRefreshTimer.Enabled)
                        lazyRefreshTimer.Enabled = true;
                }
            }
            else if (CanPaint())
                base.Refresh();
            else
                rendererNeedsPaint = true;
        }
        /// <summary>
        /// Redraws the owner control.
        /// </summary>
        /// <param name="force">If true, forces an immediate update, even if
        /// the renderer is suspended by a SuspendPaint call.</param>
        private void Refresh(bool force)
        {
            Refresh(force, false);
        }
        /// <summary>
        /// Redraws the owner control.
        /// </summary>
        private new void Refresh()
        {
            Refresh(false, false);
        }
        /// <summary>
        /// Suspends painting until a matching ResumePaint call is made.
        /// </summary>
        private void SuspendPaint()
        {
            if (rendererSuspendCount == 0) rendererNeedsPaint = false;
            rendererSuspendCount++;
        }
        /// <summary>
        /// Resumes painting. This call must be matched by a prior SuspendPaint call.
        /// </summary>
        private void ResumePaint()
        {
            System.Diagnostics.Debug.Assert(
                rendererSuspendCount > 0,
                "Suspend count does not match resume count.",
                "ResumePaint() must be matched by a prior SuspendPaint() call."
                );

            rendererSuspendCount--;
            if (rendererNeedsPaint)
                Refresh();
        }
        /// <summary>
        /// Determines if the control can be painted.
        /// </summary>
        private bool CanPaint()
        {
            if (mRenderer == null)
                return false;
            if (controlSuspended || rendererSuspendCount != 0)
                return false;
            else
                return true;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Determines whether the specified item is visible on the screen.
        /// </summary>
        /// <param name="guid">The Guid of the item to test.</param>
        /// <returns>true if the item is visible or partially visible; otherwise false.</returns>
        internal bool IsItemVisible(Guid guid)
        {
            return layoutManager.IsItemVisible(guid);
        }
        /// <summary>
        /// Determines whether the specified item is modified.
        /// </summary>
        /// <param name="guid">The Guid of the item to test.</param>
        /// <returns>true if the item is modified; otherwise false.</returns>
        internal bool IsItemDirty(Guid guid)
        {
            ImageListViewItem item = null;
            if (mItems.TryGetValue(guid, out item))
                return item.isDirty;

            return false;
        }
        /// <summary>
        /// Determines whether the specified item is visible on the screen.
        /// </summary>
        /// <param name="itemIndex">The index of the item to test.</param>
        /// <returns>An ItemVisibility value.</returns>
        internal ItemVisibility IsItemVisible(int itemIndex)
        {
            if (mItems.Count == 0) return ItemVisibility.NotVisible;
            if (itemIndex < 0 || itemIndex > mItems.Count - 1) return ItemVisibility.NotVisible;

            if (itemIndex < layoutManager.FirstPartiallyVisible || itemIndex > layoutManager.LastPartiallyVisible)
                return ItemVisibility.NotVisible;
            else if (itemIndex >= layoutManager.FirstVisible && itemIndex <= layoutManager.LastVisible)
                return ItemVisibility.Visible;
            else
                return ItemVisibility.PartiallyVisible;
        }
        /// <summary>
        /// Gets the guids of visible items.
        /// </summary>
        internal Dictionary<Guid, bool> GetVisibleItems()
        {
            Dictionary<Guid, bool> visible = new Dictionary<Guid, bool>();
            if (layoutManager.FirstPartiallyVisible != -1 && layoutManager.LastPartiallyVisible != -1)
            {
                int start = layoutManager.FirstPartiallyVisible;
                int end = layoutManager.LastPartiallyVisible;

                start -= layoutManager.Cols * layoutManager.Rows;
                end += layoutManager.Cols * layoutManager.Rows;

                start = Math.Min(mItems.Count - 1, Math.Max(0, start));
                end = Math.Min(mItems.Count - 1, Math.Max(0, end));

                for (int i = start; i <= end; i++)
                    visible.Add(mItems[i].Guid, false);
            }
            return visible;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the DragOver event.
        /// </summary>
        protected override void OnDragOver(DragEventArgs e)
        {
            navigationManager.DragOver(e);
            base.OnDragOver(e);
        }
        /// <summary>
        /// Handles the DragEnter event.
        /// </summary>
        protected override void OnDragEnter(DragEventArgs e)
        {
            navigationManager.DragEnter(e);
            base.OnDragEnter(e);
        }
        /// <summary>
        /// Handles the DragLeave event.
        /// </summary>
        protected override void OnDragLeave(EventArgs e)
        {
            navigationManager.DragLeave();
            base.OnDragLeave(e);
        }

        /// <summary>
        /// Handles the DragDrop event.
        /// </summary>
        protected override void OnDragDrop(DragEventArgs e)
        {
            navigationManager.DragDrop(e);
            base.OnDragDrop(e);
        }
        /// <summary>
        /// Handles the Scroll event of the vScrollBar control.
        /// </summary>
        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            mViewOffset.Y = e.NewValue;
            Refresh();
        }
        /// <summary>
        /// Handles the Scroll event of the hScrollBar control.
        /// </summary>
        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            mViewOffset.X = e.NewValue;
            Refresh();
        }
        /// <summary>
        /// Handles the Tick event of the lazyRefreshTimer control.
        /// </summary>
        void lazyRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (mRenderer.LazyRefreshIntervalExceeded && rendererNeedsPaint)
            {
                lazyRefreshTimer.Enabled = false;
                base.Refresh();
            }
        }
        /// <summary>
        /// Handles the Resize event.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!disposed && mRenderer != null)
                mRenderer.ClearBuffer();

            if (hScrollBar != null && layoutManager != null)
                layoutManager.Update();

            Refresh();
        }
        /// <summary>
        /// Handles the Paint event.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            rendererNeedsPaint = false;
            if (!disposed && mRenderer != null)
                mRenderer.Render(e.Graphics);
        }
        /// <summary>
        /// Handles the MouseDown event.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Capture focus if right clicked
            if (!Focused && (e.Button & MouseButtons.Right) == MouseButtons.Right)
                Focus();

            navigationManager.MouseDown(e);
            base.OnMouseDown(e);
        }
        /// <summary>
        /// Handles the MouseUp event.
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            navigationManager.MouseUp(e);
            base.OnMouseUp(e);
        }
        /// <summary>
        /// Handles the MouseMove event.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            navigationManager.MouseMove(e);
            base.OnMouseMove(e);
        }
        /// <summary>
        /// Handles the MouseWheel event.
        /// </summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            SuspendPaint();

            if (ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                int newYOffset = mViewOffset.Y - (e.Delta / SystemInformation.MouseWheelScrollDelta)
                    * vScrollBar.SmallChange;
                if (newYOffset > vScrollBar.Maximum - vScrollBar.LargeChange + 1)
                    newYOffset = vScrollBar.Maximum - vScrollBar.LargeChange + 1;
                if (newYOffset < 0)
                    newYOffset = 0;
                if (newYOffset < vScrollBar.Minimum) newYOffset = vScrollBar.Minimum;
                if (newYOffset > vScrollBar.Maximum) newYOffset = vScrollBar.Maximum;
                mViewOffset.Y = newYOffset;
                vScrollBar.Value = newYOffset;
            }
            else
            {
                int newXOffset = mViewOffset.X - (e.Delta / SystemInformation.MouseWheelScrollDelta)
                    * hScrollBar.SmallChange;
                if (newXOffset > hScrollBar.Maximum - hScrollBar.LargeChange + 1)
                    newXOffset = hScrollBar.Maximum - hScrollBar.LargeChange + 1;
                if (newXOffset < 0)
                    newXOffset = 0;
                if (newXOffset < hScrollBar.Minimum) newXOffset = hScrollBar.Minimum;
                if (newXOffset > hScrollBar.Maximum) newXOffset = hScrollBar.Maximum;
                mViewOffset.X = newXOffset;
                hScrollBar.Value = newXOffset;
            }

            OnMouseMove(e);
            Refresh(true);
            ResumePaint();

            base.OnMouseWheel(e);
        }
        /// <summary>
        /// Handles the MouseLeave event.
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            navigationManager.MouseLeave();
            base.OnMouseLeave(e);
        }
        /// <summary>
        /// Handles the MouseDoubleClick event.
        /// </summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            navigationManager.MouseDoubleClick(e);
            base.OnMouseDoubleClick(e);
        }
        /// <summary>
        /// Handles the IsInputKey event.
        /// </summary>
        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.Left) == Keys.Left ||
                (keyData & Keys.Right) == Keys.Right ||
                (keyData & Keys.Up) == Keys.Up ||
                (keyData & Keys.Down) == Keys.Down)
                return true;
            else
                return base.IsInputKey(keyData);
        }
        /// <summary>
        /// Handles the KeyDown event.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            navigationManager.KeyDown(e);
            base.OnKeyDown(e);
        }
        /// <summary>
        /// Handles the KeyUp event.
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            navigationManager.KeyUp(e);
            base.OnKeyUp(e);
        }
        /// <summary>
        /// Handles the GotFocus event.
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Refresh();
        }
        /// <summary>
        /// Handles the LostFocus event.
        /// </summary>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Refresh();
        }
        /// <summary>
        /// Releases the unmanaged resources used by the control and its child controls 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; 
        /// false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Events
                    hScrollBar.Scroll -= hScrollBar_Scroll;
                    vScrollBar.Scroll -= vScrollBar_Scroll;
                    lazyRefreshTimer.Tick -= lazyRefreshTimer_Tick;

                    // Resources
                    if (mDefaultImage != null)
                        mDefaultImage.Dispose();
                    if (mErrorImage != null)
                        mErrorImage.Dispose();
                    if (mRatingImage != null)
                        mRatingImage.Dispose();
                    if (mEmptyRatingImage != null)
                        mEmptyRatingImage.Dispose();

                    // Child controls
                    if (hScrollBar != null && hScrollBar.IsHandleCreated && !hScrollBar.IsDisposed)
                        hScrollBar.Dispose();
                    if (vScrollBar != null && vScrollBar.IsHandleCreated && !vScrollBar.IsDisposed)
                        vScrollBar.Dispose();
                    if (lazyRefreshTimer != null)
                        lazyRefreshTimer.Dispose();

                    // internal classes
                    thumbnailCache.Dispose();
                    shellInfoCache.Dispose();
                    itemCacheManager.Dispose();
                    navigationManager.Dispose();
                    if (mRenderer != null)
                        mRenderer.Dispose();
                }

                disposed = true;
            }

            if (IsHandleCreated && !IsDisposed && !InvokeRequired)
                base.Dispose(disposing);
        }
        #endregion

        #region Virtual Functions
        /// <summary>
        /// Raises the CacheError event.
        /// </summary>
        /// <param name="e">A CacheErrorEventArgs that contains event data.</param>
        protected virtual void OnCacheError(CacheErrorEventArgs e)
        {
            if (CacheError != null)
                CacheError(this, e);
        }
        /// <summary>
        /// Raises the DropFiles event.
        /// </summary>
        /// <param name="e">A DropFileEventArgs that contains event data.</param>
        protected virtual void OnDropFiles(DropFileEventArgs e)
        {
            if (DropFiles != null)
                DropFiles(this, e);

            if (e.Cancel)
                return;

            int index = e.Index;
            int firstItemIndex = 0;
            mSelectedItems.Clear(false);

            // Add items
            foreach (string filename in e.FileNames)
            {
                ImageListViewItem item = new ImageListViewItem(filename);
                item.mSelected = true;
                mItems.InsertInternal(index, item);
                if (firstItemIndex == 0) firstItemIndex = item.Index;
                index++;
            }

            EnsureVisible(firstItemIndex);
            OnSelectionChangedInternal();
        }
        /// <summary>
        /// Raises the ColumnWidthChanged event.
        /// </summary>
        /// <param name="e">A ColumnEventArgs that contains event data.</param>
        protected virtual void OnColumnWidthChanged(ColumnEventArgs e)
        {
            if (ColumnWidthChanged != null)
                ColumnWidthChanged(this, e);
        }
        /// <summary>
        /// Raises the ColumnClick event.
        /// </summary>
        /// <param name="e">A ColumnClickEventArgs that contains event data.</param>
        protected virtual void OnColumnClick(ColumnClickEventArgs e)
        {
            if (ColumnClick != null)
                ColumnClick(this, e);
        }
        /// <summary>
        /// Raises the ColumnHover event.
        /// </summary>
        /// <param name="e">A ColumnClickEventArgs that contains event data.</param>
        protected virtual void OnColumnHover(ColumnHoverEventArgs e)
        {
            if (ColumnHover != null)
                ColumnHover(this, e);
        }
        /// <summary>
        /// Raises the ItemClick event.
        /// </summary>
        /// <param name="e">A ItemClickEventArgs that contains event data.</param>
        protected virtual void OnItemClick(ItemClickEventArgs e)
        {
            if (ItemClick != null)
                ItemClick(this, e);
        }
        /// <summary>
        /// Raises the ItemCheckBoxClick event.
        /// </summary>
        /// <param name="e">A ItemEventArgs that contains event data.</param>
        protected virtual void OnItemCheckBoxClick(ItemEventArgs e)
        {
            if (ItemCheckBoxClick != null)
                ItemCheckBoxClick(this, e);
        }
        /// <summary>
        /// Raises the ItemCheckBoxClick event.
        /// </summary>
        /// <param name="item">The checked item.</param>
        internal virtual void OnItemCheckBoxClickInternal(ImageListViewItem item)
        {
            OnItemCheckBoxClick(new ItemEventArgs(item));
        }
        /// <summary>
        /// Raises the ItemHover event.
        /// </summary>
        /// <param name="e">A ItemClickEventArgs that contains event data.</param>
        protected virtual void OnItemHover(ItemHoverEventArgs e)
        {
            if (ItemHover != null)
                ItemHover(this, e);
        }
        /// <summary>
        /// Raises the ItemDoubleClick event.
        /// </summary>
        /// <param name="e">A ItemClickEventArgs that contains event data.</param>
        protected virtual void OnItemDoubleClick(ItemClickEventArgs e)
        {
            if (ItemDoubleClick != null)
                ItemDoubleClick(this, e);
        }
        /// <summary>
        /// Raises the SelectionChanged event.
        /// </summary>
        /// <param name="e">A EventArgs that contains event data.</param>
        protected virtual void OnSelectionChanged(EventArgs e)
        {
            if (SelectionChanged != null)
                SelectionChanged(this, e);
        }
        /// <summary>
        /// Raises the SelectionChanged event.
        /// </summary>
        internal void OnSelectionChangedInternal()
        {
            OnSelectionChanged(new EventArgs());
        }
        /// <summary>
        /// Raises the ThumbnailCached event.
        /// </summary>
        /// <param name="e">A ThumbnailCachedEventArgs that contains event data.</param>
        protected virtual void OnThumbnailCached(ThumbnailCachedEventArgs e)
        {
            if (ThumbnailCached != null)
                ThumbnailCached(this, e);
        }
        /// <summary>
        /// Raises the CacheError event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        /// <param name="guid">The Guid of the ImageListViewItem that is associated with this error.
        /// This parameter can be null.</param>
        /// <param name="error">The error that occurred during an asynchronous operation.</param>
        /// <param name="cacheThread">The thread raising the error.</param>
        internal void OnCacheErrorInternal(Guid guid, Exception error, CacheThread cacheThread)
        {
            ImageListViewItem item = null;
            mItems.TryGetValue(guid, out item);
            OnCacheError(new CacheErrorEventArgs(item, error, cacheThread));
        }
        /// <summary>
        /// Raises the ThumbnailCached event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        /// <param name="guid">The guid of the item whose thumbnail is cached.</param>
        /// <param name="thumbnail">The cached image.</param>
        /// <param name="size">Requested thumbnail size.</param>
        /// <param name="error">Determines whether an error occurred during thumbnail extraction.</param>
        internal void OnThumbnailCachedInternal(Guid guid, Image thumbnail, Size size, bool error)
        {
            ImageListViewItem item = null;
            if (mItems.TryGetValue(guid, out item))
                OnThumbnailCached(new ThumbnailCachedEventArgs(item, thumbnail, size, error));
        }
        /// <summary>
        /// Raises the refresh event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        internal void OnRefreshInternal()
        {
            Refresh();
        }
        /// <summary>
        /// Updates item details.
        /// This method is invoked from the item cache thread.
        /// </summary>
        internal void UpdateItemDetailsInternal(Guid guid, ImageListViewCacheMetadata.ImageMetadata info)
        {
            ImageListViewItem item = null;
            if (mItems.TryGetValue(guid, out item))
                item.UpdateDetailsInternal(info);
        }
        /// <summary>
        /// Updates item details.
        /// This method is invoked from the item cache thread.
        /// </summary>
        internal void UpdateItemDetailsInternal(Guid guid, VirtualItemDetailsEventArgs info)
        {
            ImageListViewItem item = null;
            if (mItems.TryGetValue(guid, out item))
                item.UpdateDetailsInternal(info);
        }
        /// <summary>
        /// Raises the ThumbnailCaching event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        /// <param name="guid">The guid of the item whose thumbnail is cached.</param>
        /// <param name="size">Requested thumbnail size.</param>
        internal void OnThumbnailCachingInternal(Guid guid, Size size)
        {
            ImageListViewItem item = null;
            if (mItems.TryGetValue(guid, out item))
                OnThumbnailCaching(new ThumbnailCachingEventArgs(item, size));
        }
        /// <summary>
        /// Raises the ThumbnailCaching event.
        /// </summary>
        /// <param name="e">A ThumbnailCachingEventArgs that contains event data.</param>
        protected virtual void OnThumbnailCaching(ThumbnailCachingEventArgs e)
        {
            if (ThumbnailCaching != null)
                ThumbnailCaching(this, e);
        }
        /// <summary>
        /// Raises the RetrieveVirtualItem event.
        /// </summary>
        /// <param name="e">A VirtualItemThumbnailEventArgs that contains event data.</param>
        protected virtual void OnRetrieveVirtualItemThumbnail(VirtualItemThumbnailEventArgs e)
        {
            if (RetrieveVirtualItemThumbnail != null)
                RetrieveVirtualItemThumbnail(this, e);
        }
        /// <summary>
        /// Raises the RetrieveVirtualItemImage event.
        /// </summary>
        /// <param name="e">A VirtualItemImageEventArgs that contains event data.</param>
        protected virtual void OnRetrieveVirtualItemImage(VirtualItemImageEventArgs e)
        {
            if (RetrieveVirtualItemImage != null)
                RetrieveVirtualItemImage(this, e);
        }
        /// <summary>
        /// Raises the RetrieveVirtualItemDetails event.
        /// </summary>
        /// <param name="e">A VirtualItemDetailsEventArgs that contains event data.</param>
        protected virtual void OnRetrieveVirtualItemDetails(VirtualItemDetailsEventArgs e)
        {
            if (RetrieveVirtualItemDetails != null)
                RetrieveVirtualItemDetails(this, e);
        }
        /// <summary>
        /// Raises the RetrieveVirtualItem event.
        /// This method is invoked from the thumbnail thread.
        /// </summary>
        /// <param name="e">A VirtualItemThumbnailEventArgs that contains event data.</param>
        internal virtual void RetrieveVirtualItemThumbnailInternal(VirtualItemThumbnailEventArgs e)
        {
            OnRetrieveVirtualItemThumbnail(e);
        }
        /// <summary>
        /// Raises the RetrieveVirtualItemImage event.
        /// </summary>
        /// <param name="e">A VirtualItemImageEventArgs that contains event data.</param>
        internal virtual void RetrieveVirtualItemImageInternal(VirtualItemImageEventArgs e)
        {
            OnRetrieveVirtualItemImage(e);
        }
        /// <summary>
        /// Raises the RetrieveVirtualItemDetails event.
        /// This method is called from the thumbnail thread; and runs on the thumbnail
        /// thread.
        /// </summary>
        /// <param name="e">A VirtualItemDetailsEventArgs that contains event data.</param>
        internal virtual void RetrieveVirtualItemDetailsInternal(VirtualItemDetailsEventArgs e)
        {
            OnRetrieveVirtualItemDetails(e);
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Occurs when an error occurs during an asynchronous cache operation.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when an error occurs during an asynchronous cache operation.")]
        public event CacheErrorEventHandler CacheError;
        /// <summary>
        /// Occurs after the user drops files on to the control.
        /// </summary>
        [Category("Drag Drop"), Browsable(true), Description("Occurs after the user drops files on to the control.")]
        public event DropFilesEventHandler DropFiles;
        /// <summary>
        /// Occurs after the user successfully resized a column header.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs after the user successfully resized a column header.")]
        public event ColumnWidthChangedEventHandler ColumnWidthChanged;
        /// <summary>
        /// Occurs when the user clicks a column header.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user clicks a column header.")]
        public event ColumnClickEventHandler ColumnClick;
        /// <summary>
        /// Occurs when the user moves the mouse over (and out of) a column header.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user moves the mouse over (and out of) a column header.")]
        public event ColumnHoverEventHandler ColumnHover;
        /// <summary>
        /// Occurs when the user clicks an item.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user clicks an item.")]
        public event ItemClickEventHandler ItemClick;
        /// <summary>
        /// Occurs when the user clicks an item checkbox.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user clicks an item checkbox.")]
        public event ItemCheckBoxClickEventHandler ItemCheckBoxClick;
        /// <summary>
        /// Occurs when the user moves the mouse over (and out of) an item.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user moves the mouse over (and out of) an item.")]
        public event ItemHoverEventHandler ItemHover;
        /// <summary>
        /// Occurs when the user double-clicks an item.
        /// </summary>
        [Category("Action"), Browsable(true), Description("Occurs when the user double-clicks an item.")]
        public event ItemDoubleClickEventHandler ItemDoubleClick;
        /// <summary>
        /// Occurs when the selected items collection changes.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when the selected items collection changes.")]
        public event EventHandler SelectionChanged;
        /// <summary>
        /// Occurs after an item thumbnail is cached.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs after an item thumbnail is cached.")]
        public event ThumbnailCachedEventHandler ThumbnailCached;
        /// <summary>
        /// Occurs before an item thumbnail is cached.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs before an item thumbnail is cached.")]
        public event ThumbnailCachingEventHandler ThumbnailCaching;
        /// <summary>
        /// Occurs when thumbnail image for a virtual item is requested.
        /// The lifetime of the image will be controlled by the control.
        /// This event will be run in the worker thread context.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when thumbnail image for a virtual item is requested.")]
        public event RetrieveVirtualItemThumbnailEventHandler RetrieveVirtualItemThumbnail;
        /// <summary>
        /// Occurs when source image for a virtual item is requested.
        /// The lifetime of the image will be controlled by the control.
        /// This event will be run in the worker thread context.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when source image for a virtual item is requested.")]
        public event RetrieveVirtualItemImageEventHandler RetrieveVirtualItemImage;
        /// <summary>
        /// Occurs when details of a virtual item are requested.
        /// This event will be run in the worker thread context.
        /// </summary>
        [Category("Behavior"), Browsable(true), Description("Occurs when details of a virtual item are requested.")]
        public event RetrieveVirtualItemDetailsEventHandler RetrieveVirtualItemDetails;
        #endregion
    }
}