﻿using CRMResourceSynchronization.Core.Business;
using CRMResourceSynchronization.Core.Business.Models;
using CRMResourceSynchronization.Core.Dynamics;
using CRMResourceSynchronization.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static CRMResourceSynchronization.Core.Dynamics.CRMClient;

namespace CRMResourceSynchronization
{
    /// <summary>
    /// Interaction logic for MainControl.
    /// </summary>
    public partial class CompareResourceWindowControl : UserControl
    {
        private CRMClient CRMClient = null;
        public SolutionsBusiness solutions { get; set; }
        private List<SolutionModel> listSolutions = new List<SolutionModel>();
        public ResourcesBusiness resources { get; set; }
        private List<ResourceModel> listResources = new List<ResourceModel>();

        private int pageIndex = 1;
        private int numberOfRecPerPage;
        //To check the paging direction according to use selection.
        private enum PagingMode  { First = 1, Next = 2, Previous = 3, Last = 4, PageCountChange = 5, Refresh = 6 };

        private int CRMTypeResourceSelected = 0;
        private string CRMNameSearchResourceSelected = "";

        public string CRMNameSearchResourceDefaultText = "Search by resource name";

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareResourceWindowControl"/> class.
        /// </summary>
        public CompareResourceWindowControl()
        {
            this.InitializeComponent();
            
            CRMNameSearchResource.Text = CRMNameSearchResourceDefaultText;
            loading.Visibility = Visibility.Hidden;

            ActionsOfSolutions(Visibility.Hidden);
            SetEnvironment();
        }

        private bool connectCRM()
        {
            AuthenticationParameters authenticationParameters = new AuthenticationParameters();

            authenticationParameters._CRMUrl = Settings.Default.CRMUrl;
            authenticationParameters._CRMUserName = Settings.Default.CRMUserName;
            authenticationParameters._CRMPassword = Settings.Default.CRMPassword;
            authenticationParameters._ClientId = Settings.Default.ClientId;
            authenticationParameters._RedirectUri = Settings.Default.RedirectUri;
            authenticationParameters._TokenCacheStorePath = Settings.Default.TokenCacheStorePath;
            authenticationParameters._TokenCacheStorePath = Settings.Default.TokenCacheStorePath;
            authenticationParameters._IntegratedSecurityPrompt = Settings.Default.IntegratedSecurityPrompt;
            authenticationParameters._AuthLoginPrompt = Settings.Default.AuthLoginPrompt;
            authenticationParameters._CertificateThumprint = Settings.Default.CertificateThumprint;
            authenticationParameters._ClientSecret = Settings.Default.ClientSecret;
            authenticationParameters._AuthenticationType = Utils.GetObjectEnumFromDescription<AutenticationType>(Settings.Default.CRMTypeAuth);

            CRMClient = new CRMClient(authenticationParameters);
            if(CRMClient.GetOrganizationService())
                return true;
            else
                return false;
        }


        private void ConfiEnvironment_Click(object sender, RoutedEventArgs e)
        {
            Window w = new Window();
            w.Title = "Setting up the CRM environment";
            w.Content = new LoginWindowControl();
            w.Width = this.ActualWidth;
            w.Height = this.ActualHeight;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.ShowDialog();
        }

        private void ConfigPaths_Click(object sender, RoutedEventArgs e)
        {
            Window w = new Window();
            w.Title = "Paths local of resources type";
            w.Content = new PathsWindowControl();
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.ShowDialog();
        }

        private void CRMLoadSolutions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (connectCRM())
                {
                    CRMSolutions.ItemsSource = null;
                    CRMLoadSolutionsAsync();                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to retrieve CRM solutions : '{0}'", ex.Message));
            }
        }

        private async Task<bool> CRMLoadSolutionsAsync()
        {
            bool finish = false;
            loading.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                solutions = new SolutionsBusiness(CRMClient, reloadSettingsToModel());
                listSolutions = solutions.GetSolutionsManaged();
                if (listSolutions.Count > 0)
                {
                    listSolutions.Insert(0, new SolutionModel() { solutionid = "", friendlyname = "-- Select a solution --" });
                    finish = true;
                }                
            }).ContinueWith(resp => {
                
            });

            loading.Visibility = Visibility.Hidden;

            if (finish)
            {
                ActionsOfSolutions(Visibility.Visible);
            }
            else
            {
                ActionsOfSolutions(Visibility.Hidden);
            }

            return finish;
        }

        private void CRMSolutions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CRMLoadResourcesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to retrieve CRM solution resources : {0} - {0}", ((SolutionModel)CRMSolutions.SelectedItem).solutionid, ex.Message));
            }
        }

        private async Task<bool> CRMLoadResourcesAsync()
        {
            bool finish = false;
            loading.Visibility = Visibility.Visible;

            listResources.Clear();

            if (CRMSolutions.SelectedItem != null && CRMSolutions.SelectedIndex > 0)
            {
                Guid solutionParse = Guid.Empty;
                if (Guid.TryParse(((SolutionModel)CRMSolutions.SelectedItem).solutionid, out solutionParse))
                {
                    await Task.Run(() =>
                    {
                        if (connectCRM())
                        {
                            resources = new ResourcesBusiness(CRMClient, reloadSettingsToModel());
                            listResources = resources.GetResourcesFromSolution(solutionParse);
                            listResources = listResources.OrderBy(k => k.name).ToList();
                            if (listResources.Count > 0)
                            {
                                finish = true;
                            }
                        }
                    }).ContinueWith(resp => {

                    });                   
                }
                else
                {
                    ActionsOfResources(Visibility.Hidden);
                }
            }
            else
            {
                ActionsOfResources(Visibility.Hidden);
            }

            loading.Visibility = Visibility.Hidden;

            if (finish)
            {
                Navigate((int)PagingMode.PageCountChange);
                ActionsOfResources(Visibility.Visible);
                CRMTypeResource.SelectedIndex = 0;
            }
            else
            {
                ActionsOfResources(Visibility.Hidden);
                CRMSolutions.SelectedIndex = 0;
            }

            return finish;
        }

        private SettingsModel reloadSettingsToModel()
        {
            SettingsModel sm = new SettingsModel();
            sm.CRMUrl = Settings.Default.CRMUrl;
            sm.CRMUserName = Settings.Default.CRMUserName;
            sm.CRMPassword = Settings.Default.CRMPassword;
            sm.ClientId = Settings.Default.ClientId;
            sm.RedirectUri = Settings.Default.RedirectUri;
            sm.TokenCacheStorePath = Settings.Default.TokenCacheStorePath;
            sm.IntegratedSecurityPrompt = Settings.Default.IntegratedSecurityPrompt;
            sm.AuthLoginPrompt = Settings.Default.AuthLoginPrompt;
            sm.CertificateThumprint = Settings.Default.CertificateThumprint;
            sm.ClientSecret = Settings.Default.ClientSecret;
            sm.CRMTypeAuth = Settings.Default.CRMTypeAuth;
            sm.PathHTML = Settings.Default.PathHTML;
            sm.PathCSS = Settings.Default.PathCSS;
            sm.PathJS = Settings.Default.PathJS;
            sm.PathXML = Settings.Default.PathXML;
            sm.PathPNG = Settings.Default.PathPNG;
            sm.PathJPG = Settings.Default.PathJPG;
            sm.PathGIF = Settings.Default.PathGIF;
            sm.PathXAP = Settings.Default.PathXAP;
            sm.PathXSL = Settings.Default.PathXSL;
            sm.PathICO = Settings.Default.PathICO;
            sm.PathSVG = Settings.Default.PathSVG;
            sm.PathRESX = Settings.Default.PathRESX;

            return sm;
        }

        private void ActionsOfResources(Visibility type)
        {
            if (type == Visibility.Visible)
            {
                CRMNameSearchResource.IsEnabled = true;
                CRMTypeResource.IsEnabled = true;
                CRMNameSearchResource.IsEnabled = true;
                CRMNameSearchResource.Opacity = 1;
                CRMSearchResource.IsEnabled = true;
                ScrollDataSources.Visibility = Visibility.Visible;
                GridResourceActions.Visibility = Visibility.Visible;
                GridPagination.Visibility = Visibility.Visible;
                DataResources.Visibility = Visibility.Visible;                
            }
            else
            {
                DataResources.ItemsSource = null;
                CRMNameSearchResource.IsEnabled = false;
                CRMTypeResource.IsEnabled = false;
                CRMNameSearchResource.IsEnabled = false;
                CRMNameSearchResource.Opacity = 0.4;
                CRMSearchResource.IsEnabled = false;
                ScrollDataSources.Visibility = Visibility.Hidden;
                GridResourceActions.Visibility = Visibility.Hidden;
                GridPagination.Visibility = Visibility.Hidden;
                DataResources.Visibility = Visibility.Hidden;
            }
            ActionsOfActionsOfResources();
        }

        private void ActionsOfActionsOfResources()
        {
            bool enableBtnUploadAndPublishResource = true;
            bool enableBtnCompareResource = true;

            foreach (var resource in listResources.Where(k => k.selectResource))
            {
                if (enableBtnUploadAndPublishResource && String.IsNullOrEmpty(resource.pathlocal))
                    enableBtnUploadAndPublishResource = false;

                if (enableBtnCompareResource && String.IsNullOrEmpty(resource.contentLocal))
                    enableBtnUploadAndPublishResource = false;
            }

            //Enabled/Disabled button donwload resources
            if (listResources.Where(k => k.selectResource).Count() > 0)
            {
                CRMDownloadResource.Opacity = 1;
                CRMDownloadResource.IsEnabled = true;
                CRMDownloadResource.Visibility = Visibility.Visible;
                if (enableBtnUploadAndPublishResource)
                {
                    CRMCompareResources.Opacity = 1;
                    CRMCompareResources.IsEnabled = true;
                }
            }
            else
            {
                CRMDownloadResource.Opacity = 0.4;
                CRMDownloadResource.IsEnabled = false;
                CRMCompareResources.Opacity = 0.4;
                CRMCompareResources.IsEnabled = false;                
            }

            //Enabled/Disabled button upload and publish resources
            if (listResources.Where(k => k.selectResource).Count() > 0 && enableBtnUploadAndPublishResource)
            {
                CRMUploadResource.IsEnabled = true;
                CRMUploadResource.Opacity = 1;
                CRMPublishResource.IsEnabled = true;
                CRMPublishResource.Opacity = 1;
            }
            else
            {
                CRMUploadResource.Opacity = 0.4;
                CRMUploadResource.IsEnabled = false;
                CRMPublishResource.Opacity = 0.4;
                CRMPublishResource.IsEnabled = false;
            }
        }

        private void ActionsOfSolutions(Visibility type)
        {
            if (type == Visibility.Visible)
            {
                CRMSolutions.ItemsSource = listSolutions.OrderBy(k => k.friendlyname);
                CRMSolutions.SelectedValue = "solutionid";
                CRMSolutions.SelectedItem = "friendlyname";
                CRMSolutions.DisplayMemberPath = "friendlyname";
                CRMSolutions.SelectedValuePath = "solutionid";
                CRMSolutions.IsEnabled = true;
                CRMSolutions.SelectedIndex = 0;
            }
            else
            {
                CRMSolutions.IsEnabled = false;
                CRMSolutions.SelectedIndex = 0;
                ActionsOfResources(Visibility.Hidden);
            }
        }

        private void SetEnvironment()
        {
            CRMUser.Text = Settings.Default.CRMUserName;
            CRMUrl.Text = Settings.Default.CRMUrl;
        }

        #region Pagination 
        private void btnFirst_Click(object sender, System.EventArgs e)
        {
            Navigate((int)PagingMode.First);
        }

        private void btnNext_Click(object sender, System.EventArgs e)
        {
            Navigate((int)PagingMode.Next);
        }

        private void btnPrev_Click(object sender, System.EventArgs e)
        {
            Navigate((int)PagingMode.Previous);
        }

        private void btnLast_Click(object sender, System.EventArgs e)
        {
            Navigate((int)PagingMode.Last);
        }

        private void cbNumberOfRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Navigate((int)PagingMode.PageCountChange);
        }

        private void Navigate(int mode)
        {
            int from = 0;
            int to = 0;
            
            switch (mode)
            {
                case (int)PagingMode.Next:
                    btnPrev.IsEnabled = true;
                    btnPrev.Opacity = 1;
                    btnFirst.IsEnabled = true;
                    btnFirst.Opacity = 1;                   

                    DataResources.ItemsSource = null;

                    from = (pageIndex * numberOfRecPerPage);

                    pageIndex += 1;

                    if (listResources.Count >= ((pageIndex * numberOfRecPerPage) + numberOfRecPerPage))
                    {                       
                        DataResources.ItemsSource = listResources.GetRange(pageIndex * numberOfRecPerPage, numberOfRecPerPage);
                        to = (pageIndex * numberOfRecPerPage);
                    }
                    else
                    {
                        DataResources.ItemsSource = listResources.GetRange(pageIndex * numberOfRecPerPage, listResources.Count - (pageIndex * numberOfRecPerPage));
                        from = pageIndex * numberOfRecPerPage;
                        to = listResources.Count;
                    }

                    lblpageInformation.Content = from + " to " + to + " of " + listResources.Count;

                    if (((pageIndex * numberOfRecPerPage) + numberOfRecPerPage) > listResources.Count)
                    {
                        btnNext.IsEnabled = false;
                        btnNext.Opacity = 0.4;
                        btnLast.IsEnabled = false;
                        btnLast.Opacity = 0.4;
                    }

                    break;
                case (int)PagingMode.Previous:
                    btnNext.IsEnabled = true;
                    btnNext.Opacity = 1;
                    btnLast.IsEnabled = true;
                    btnLast.Opacity = 1;
                    if (pageIndex > 1)
                    {
                        pageIndex -= 1;
                        DataResources.ItemsSource = null;
                        if (pageIndex == 1)
                        {
                            DataResources.ItemsSource = listResources.GetRange(0, numberOfRecPerPage);
                            from = 1;
                            to = listResources.GetRange(0, numberOfRecPerPage).Count;
                        }
                        else
                        {
                            DataResources.ItemsSource = listResources.GetRange(pageIndex * numberOfRecPerPage, numberOfRecPerPage);
                            from = (pageIndex * numberOfRecPerPage);
                            to = (pageIndex * numberOfRecPerPage) + numberOfRecPerPage;
                        }
                    }

                    lblpageInformation.Content =  from + " to " + to + " of " + listResources.Count;

                    if (pageIndex <= 1)
                    {
                        btnPrev.IsEnabled = false;
                        btnPrev.Opacity = 0.4;
                        btnFirst.IsEnabled = false;
                        btnFirst.Opacity = 0.4;
                    }
                    break;

                case (int)PagingMode.First:
                    pageIndex = 2;
                    Navigate((int)PagingMode.Previous);
                    break;
                case (int)PagingMode.Last:
                    if (numberOfRecPerPage > 0)
                    {
                        pageIndex = (listResources.Count / numberOfRecPerPage) - 1;
                        Navigate((int)PagingMode.Next);
                    }
                    break;
                case (int)PagingMode.PageCountChange:
                    pageIndex = 1;
                    numberOfRecPerPage = Convert.ToInt32(((ComboBoxItem)cbNumberOfRecords.SelectedItem).Content);
                    DataResources.ItemsSource = null;
                    if (listResources.Count > 0)
                    {
                        if (numberOfRecPerPage < listResources.Count)
                        {
                            DataResources.ItemsSource = listResources.GetRange(0, numberOfRecPerPage);
                            to = listResources.GetRange(0, numberOfRecPerPage).Count;
                        }
                        else
                        {
                            DataResources.ItemsSource = listResources.GetRange(0, listResources.Count);
                            to = listResources.GetRange(0, listResources.Count).Count;
                        }
                        lblpageInformation.Content = to + " de " + listResources.Count;
                    }

                    if (listResources.Count <= (pageIndex * numberOfRecPerPage))
                    {
                        btnNext.IsEnabled = false;
                        btnNext.Opacity = 0.4;
                        btnLast.IsEnabled = false;
                        btnLast.Opacity = 0.4;
                    }
                    else
                    {
                        btnNext.IsEnabled = true;
                        btnNext.Opacity = 1;
                        btnLast.IsEnabled = true;
                        btnLast.Opacity = 1;
                    }

                    btnPrev.IsEnabled = false;
                    btnPrev.Opacity = 0.4;
                    btnFirst.IsEnabled = false;
                    btnFirst.Opacity = 0.4;
                    break;
                case (int)PagingMode.Refresh:
                    List<ResourceModel> newData = new List<ResourceModel>();
                    if (pageIndex > 1)
                    {
                        newData = listResources.GetRange(((pageIndex -1) * numberOfRecPerPage), listResources.Count - ((pageIndex - 1) * numberOfRecPerPage));
                    }
                    else
                    {
                        if (numberOfRecPerPage < listResources.Count)
                        {
                            newData = listResources.GetRange(0, numberOfRecPerPage);
                        }
                        else
                        {
                            newData = listResources.GetRange(0, listResources.Count);
                        }                        
                    }

                    if(CRMTypeResourceSelected > 0)
                    {
                        newData = newData.Where(k => k.webresourcetype == CRMTypeResourceSelected).ToList();
                    }

                    if (!String.IsNullOrEmpty(CRMNameSearchResourceSelected))
                    {
                        newData = newData.Where(k => k.name.Contains(CRMNameSearchResourceSelected)).ToList();
                    }

                    DataResources.ItemsSource = newData;

                    lblpageInformation.Content = newData.Count + " to " + listResources.Count;

                    break;
            }            
        }
        #endregion

        #region Events to Grid
        /// <summary>
        /// Detect resource selection in header
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (ResourceModel c in listResources)
            {
                c.selectResource = true;
            }
            Navigate((int)PagingMode.Refresh);
            ActionsOfActionsOfResources();
        }
        /// <summary>
        ///  Detect resource deselection in header
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (ResourceModel c in listResources)
            {
                c.selectResource = false;
            }
            Navigate((int)PagingMode.Refresh);
            ActionsOfActionsOfResources();
        }
        private void ResourceChecked(object sender, RoutedEventArgs e)
        {
            ((ResourceModel)((DataGridCell)sender).DataContext).selectResource = true;
            ActionsOfActionsOfResources();
        }
        private void ResourceUnChecked(object sender, RoutedEventArgs e)
        {
            ((ResourceModel)((DataGridCell)sender).DataContext).selectResource = false;
            ActionsOfActionsOfResources();
        }
        /// <summary>
        /// Allow resource selection when selecting the resource line in grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row == null) return;
            if (row.IsEditing) return;
            if (!row.IsSelected) row.IsSelected = true;
        }
        /// <summary>
        /// Allow resource selection when selecting the resource line in grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell == null) return;
            if (cell.IsEditing) return;
            if (!cell.IsFocused) cell.Focus();
        }

        #endregion

        #region Search TextBox
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchResourceTextInTextBox(sender);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchResourceTextInTextBox(sender);
        }

        private void SearchResourceTextInTextBox(object sender)
        {
            TextBox tb = ((TextBox)sender);
            if (tb.IsFocused)
            {
                if (tb.Text == CRMNameSearchResourceDefaultText)
                {
                    tb.Text = "";
                }
            }
            else
            {
                if (tb.Text == "")
                {
                    tb.Text = CRMNameSearchResourceDefaultText;
                    CRMNameSearchResourceSelected = "";
                }
            }
        }
        #endregion

        private void CRMSearchResource_Click(object sender, RoutedEventArgs e)
        {
            if (CRMTypeResource.SelectedItem != null && CRMTypeResource.SelectedIndex > 0) {
                CRMTypeResourceSelected = Convert.ToInt32(((ComboBoxItem)CRMTypeResource.SelectedItem).Tag);
            }
            if(CRMNameSearchResource.Text != CRMNameSearchResourceDefaultText)
            {
                CRMNameSearchResourceSelected = CRMNameSearchResource.Text;
            }

            Navigate((int)PagingMode.Refresh);
        }

        private void CRMCompareResources_Click(object sender, RoutedEventArgs e)
        {
            Window w = new Window();
            w.Title = "Differences of resources";
            w.Content = new DifferencesResourceWindowControlControl(listResources.Where(k => k.selectResource == true).ToList());
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.ShowDialog();
        }

        private void CRMDownloadResource_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}