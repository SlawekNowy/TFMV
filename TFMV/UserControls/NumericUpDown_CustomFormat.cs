//based on https://www.codeproject.com/Articles/29244/A-Derived-NumericUpDown-that-Provides-Handlers-for
//thanks Rafique Sheikh!


//todo: we might not need this at all, could just access the textbox control inside the original numericUpDown and handle it all like that?


using Microsoft.JScript;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace DeviceCtrlLibrary
{    
    public class NumericUpDown_CustomFormat : NumericUpDown
    {

        protected override void ValidateEditText()
        {
            if (base.UserEdit)
            {
                base.ValidateEditText();
            }
        }

        protected override void UpdateEditText()
        {
            //todo: expose this is as a custom property
            Text = base.Value.ToString("0.#####");
        }



        /*
        public event EventHandler UpButtonClicked = null;
        public event EventHandler DownButtonClicked = null;

        public void OnUpButtonClicked(EventArgs e)
        {
            EventHandler eventCopy = UpButtonClicked;
            if (eventCopy != null)
                eventCopy(this, e);
        }

        public void OnDownButtonClicked(EventArgs e)
        {
            EventHandler eventCopy = DownButtonClicked;
            if (eventCopy != null)
                eventCopy(this, e);
        }

        public NumericUpDnWithUpDnBtnEventHandlers()
        {            
        }

        public override void DownButton()
        {
            try
            {                
                OnDownButtonClicked(new EventArgs());
            }
            catch (Exception ex)
            {                
                Console.WriteLine(ex.Message);                
            }
            base.DownButton();
        }

        public override void UpButton()
        {
            try
            {               
                OnUpButtonClicked(new EventArgs());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            base.UpButton();
        }
        */
    }
}
