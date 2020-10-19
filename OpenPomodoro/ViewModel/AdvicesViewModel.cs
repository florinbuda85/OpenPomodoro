using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PomodoroDatabase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OpenPomodoro.ViewModel
{
    public class AdvicesViewModel : ViewModelBase
    {
        public AdvicesViewModel()
        {
            if (!IsInDesignModeStatic)
            {
                PopulateTheAdviceList();
            }
        }

        public string NewAdvice { get; set; }
        private PauseAdvice _SelectedAdvice;

        public PauseAdvice SelectedAdvice
        {
            get { return _SelectedAdvice; }
            set { _SelectedAdvice = value; IsEditing = false; ResetAdvice();  }
        }
        public bool IsEditing { get; set; }


        public ObservableCollection<PauseAdvice> TheAdviceList { get; set; }

        private void PopulateTheAdviceList()
        {
            try
            {
                TheAdviceList = new ObservableCollection<PauseAdvice>();

                var advices = DBSingleton.getInstance().GetAllAdvices();

                if (advices != null && advices.Count > 0)
                {
                    advices.ForEach(x => TheAdviceList.Add(x));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"PopulateTheAdviceList: {e.Message}");
            }
            finally
            {
                RaisePropertyChanged("TheAdviceList");
                ResetAdvice();
                DBSingleton.getInstance().ResetAdviceViews(); // warning: bug generator!
            }
        }

        private void ResetAdvice()
        {
            NewAdvice = "";
            RaisePropertyChanged("NewAdvice");
        }

        #region ICommand DoDelete
        private ICommand _DoDelete;
        public ICommand DoDelete
        {
            get
            {
                if (_DoDelete is null)
                {
                    _DoDelete = new RelayCommand(DoDeleteExecute); // RelayCommand
                }
                return _DoDelete;
            }
        }
        private void DoDeleteExecute()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                if (SelectedAdvice != null)
                {
                    if (MessageBox.Show("Do you confirm?", "???", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        DBSingleton.getInstance().DeleteAdvice(SelectedAdvice.id);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Exception found on DoDelete :" + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                PopulateTheAdviceList();
            }
        }
        #endregion

        #region ICommand DoEdit
        private ICommand _DoEdit;
        public ICommand DoEdit
        {
            get
            {
                if (_DoEdit is null)
                {
                    _DoEdit = new RelayCommand(DoEditExecute); // RelayCommand
                }
                return _DoEdit;
            }
        }
        private void DoEditExecute()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                if (SelectedAdvice != null)
                {
                    IsEditing = true;
                    NewAdvice = SelectedAdvice.Content;
                    RaisePropertyChanged("NewAdvice");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Exception found on DoEdit :" + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
        }
        #endregion

        #region ICommand DoSave
        private ICommand _DoSave;
        public ICommand DoSave
        {
            get
            {
                if (_DoSave is null)
                {
                    _DoSave = new RelayCommand(DoSaveExecute); // RelayCommand
                }
                return _DoSave;
            }
        }
        private void DoSaveExecute()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                if (IsEditing)
                {
                    if (SelectedAdvice != null)
                    {
                        DBSingleton.getInstance().DeleteAdvice(SelectedAdvice.id);
                    }
                }
                DBSingleton.getInstance().InsertAdvice(NewAdvice);
                IsEditing = false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Exception found on DoSave :" + ex.Message);
            }
            finally
            {
                PopulateTheAdviceList();
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
        }
        #endregion
    }
}
