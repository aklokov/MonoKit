//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SimpleElement.cs" company="sgmunn">
//    (c) sgmunn 2012  
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//    documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//    to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
//    the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//    THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//    IN THE SOFTWARE.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace MonoKit.ViewModels
{
    using System;

    public interface IViewModelStyle
    {
       // string ViewType { get; }

        /*
         * Need to get the view, bind the view to the view model
         * here we need to inject any behaviours that we might want
         * /
         * */


//        Type ViewType { get; }
//        void Bind(object target, object dataContext);
//        object Param { get; }
//        List<Type> Behaviours { get; }
//        bool Renders(object data);
    }




    public class X{}
    // todo: put a command (tap) on this or viewmodel base
  
    // query - add binding scope for binding data objects to this
    public abstract class SimpleElement : ViewModelBase
    {
        private string text;

        public SimpleElement()
        {
        }
        
        public string Text
        {
            get
            {
                return this.text;
            }
            
            set
            {
                if (this.text != value)
                {
                    this.text = value;
                    this.NotifyPropertyChanged("Text");
                }
            }
        }
    }
}

