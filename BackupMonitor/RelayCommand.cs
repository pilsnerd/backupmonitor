using System;
using System.Windows.Input;

public class RelayCommand : ICommand
{

    private readonly Func<bool> _canExecute;
    private readonly Action<object> _execute;

    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
    }

    public RelayCommand(Action<object> execute, Func<bool> canExecute)
    {
        if (execute == null)
        {
            throw new ArgumentNullException("execute");
        }
        _execute = execute;
        _canExecute = canExecute;
    }

    //private readonly Action _handler;
    //public RelayCommand(Action handler)
    //{
    //	_handler = handler;
    //}

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            if (value != _isEnabled)
            {
                _isEnabled = value;
                if (CanExecuteChanged != null)
                {
                    CanExecuteChanged(this, EventArgs.Empty);
                }
            }
        }
    }

    public bool CanExecute(object parameter)
    {
        return IsEnabled;
    }

    public event EventHandler CanExecuteChanged;

    public void Execute(object parameter)
    {
        _execute(parameter);
        //_handler();
    }
}


//public class RelayCommand : ICommand
//{
//    private Action _handler;
//    public RelayCommand(Action handler)
//    {
//        _isEnabled = true;//default to true
//        _handler = handler;
//    }

//    private bool _isEnabled;
//    public bool IsEnabled
//    {
//        get { return _isEnabled; }
//        set
//        {
//            if (value != _isEnabled)
//            {
//                _isEnabled = value;
//                if (CanExecuteChanged != null)
//                {
//                    CanExecuteChanged(this, EventArgs.Empty);
//                }
//            }
//        }
//    }

//    public bool CanExecute(object parameter)
//    {
//        return IsEnabled;
//    }

//    public event EventHandler CanExecuteChanged;

//    public void Execute(object parameter)
//    {
//        _handler();
//    }
//}


