using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


//TODO: A LOT OF THIS SHOULD BE PRIVATE!

//TODO: LIMIT TO THREE DECIMAL PLACES TO HIDE IMPRECISION! MAYBE ROUND UP NUMBERS THAT END IN .999! maybe done?

//TODO: WHEN WRITING JIGGLEBONE DATA, CONSIDER MOVING THEM ALL TO THE END OF THE FILE AND RE-POINTING THE OFFSETS! BECAUSE IS_BOING IS WRITING OVER 2 BYTES OF UNKNOWN DATA!!


namespace TFMV.UserControls.Jigglebone_Editor
{

    using ExtensionMethods;
//    using System.Security.Policy;
    using System.Threading;
//    using System.Windows.Media.Media3D;

    



    public partial class AddJiggleBone : Form
    {

        /*

        public class TestNum : NumericUpDown
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
                Text = Convert.ToInt32(base.Value).ToString("00");
            }
        }
        */


        //public Form form_AddJigglebone;

        
        public Thread always_on_top_thread = new Thread(() =>
                {
                });


        private Point NULL_PROPERTY_PANEL_LOCATION = new Point(1000, 1000); //out of bounds ;)
        private Point LEFT_PROPERTY_PANEL_LOCATION = new Point(12, 97);
        private Point RIGHT_PROPERTY_PANEL_LOCATION = new Point(232, 97);

        private bool resetting_all_jigglebone_values = true;


        //FLAGS!!!
        public const int JIGGLE_IS_FLEXIBLE = 0x01;
        public const int JIGGLE_IS_RIGID = 0x02;
        public const int JIGGLE_HAS_YAW_CONSTRAINT = 0x04;
        public const int JIGGLE_HAS_PITCH_CONSTRAINT = 0x08;
        public const int JIGGLE_HAS_ANGLE_CONSTRAINT = 0x10;
        public const int JIGGLE_HAS_LENGTH_CONSTRAINT = 0x20;
        public const int JIGGLE_HAS_BASE_SPRING = 0x40;
        public const int JIGGLE_IS_BOING = 0x80;


        //TODO: this is a copy of the one in TFMV.Main and both should be moved to a more generic functions script

        //this method makes sure the user can't type invalid stuff into textboxes intended for numbers. allows one decimal point and negative numbers.
        private void textBoxNumeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.') && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }

            //get the TEXTBOX component of the NumericUpDown
            TextBox numericUpDown = (TextBox)(sender as NumericUpDown).Controls[1];

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((numericUpDown).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }

            //MessageBox.Show("" + (sender as TextBox).SelectionStart);

            // allow negative numbers
            if ((e.KeyChar == '-'))
            {
                //if we already have a - anywhere in the number, ignore the keypress
                if (numericUpDown.Text.IndexOf('-') > -1)
                {
                    e.Handled = true;
                }
                else
                //if the user is attempting to put a - anywhere but the start of the string, ignore the keypress
                {
                    if (numericUpDown.SelectionStart != 0)
                    {
                        e.Handled = true;
                    }
                }
            }

        }



        public List<jiggleBone> allJiggleBones = new List<jiggleBone>();

        //currently loaded mdl
        public byte[] mdl_data = { };

        //currently loaded mdl path
        public string mdlpath = "";

        public AddJiggleBone()
        {
            InitializeComponent();
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void AddJiggleBone_Shown(object sender, EventArgs e)
        {
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button12_Click(object sender, EventArgs e)
        {

        }


        private string ReadNullTerminatedString(BinaryReader inputFileReader)
        {
            StringBuilder text = new StringBuilder();

            text.Length = 0;

            while (inputFileReader.PeekChar() > 0)
            {

                text.Append(inputFileReader.ReadChar());
            }

            //' Read the null character.

            try
            {
                inputFileReader.ReadChar();
            }
            catch (IOException)
            {
                //return "<INVALID>";
            }

            return text.ToString();

        }


        public void readJigglebones()
        {

            
            //todo: move this somewhere else, like form_load
            always_on_top_thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        if (chk_Always_On_Top.Checked)
                        {
                            this.Invoke(new MethodInvoker(delegate { this.TopMost = true; }));
                        }
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex) { }
            });

            always_on_top_thread.Start();
            

            //test item
            string filepath = "C:\\Users\\jburn\\Documents\\Crowbar\\models\\jiggletest_135\\oktoberfester_jiggletest.mdl"; //NOTE WHEN YOU REMOVE THIS YOU NEED TO REMOVE IT ELSEWHERE IN THIS FILE TOO OR IT WONT SAVE HEX EDITED BONES CORRECTLY!

            //            MessageBox.Show(filepath);

            //string filepath = TFMV.Main.tmp_dir + mdlpath;

            if (File.Exists(filepath))
            {
                //todo: uncomment this
                //try
                //{
                mdl_data = File.ReadAllBytes(filepath);

                //156 is the offset to the bone count
                int bone_count = BitConverter.ToInt32(mdl_data, 156);

                //MessageBox.Show("how many bones? this many: " + bone_count);

                //neodement: 160 is the offset to the first bone in the bone data (seems to always be 664)
                int bone_offset = BitConverter.ToInt32(mdl_data, 160);


                //iterate over each bone
                for (int i = 1; i <= bone_count; i++)

                {


                    //check if it's a jigglebone or not
                    //the 164th byte is the procedural bone type.
                    int bone_type = BitConverter.ToInt32(mdl_data, bone_offset + 164);



                    //if bone type is jigglebone (05), carry on.
                    if (bone_type == 0x05)
                    {




                        int bone_names_list_offset = BitConverter.ToInt32(mdl_data, 348);
                        //int bone_names_list_length = mdl_data.Length - bone_names_list_offset;


                        //the first int32 in each bone is the offset to its entry in the name table
                        int bone_name_offset = BitConverter.ToInt32(mdl_data, bone_offset) + bone_offset;

                        //this stops reading strings from attempting to seek past the end of the file
                        //int numBytesToRead = mdl_data.Length - bone_name_offset;

                        //string bone_name = Encoding.Default.GetString(mdl_data, bone_name_offset, numBytesToRead);


                        int jiggleboneDataOffset = BitConverter.ToInt32(mdl_data, bone_offset + 168);

                        jiggleboneDataOffset += bone_offset;

                        //todo: the entire file should use this stream, no need to load it twice

                        //make a new serialized jigglebone object to set params
                        jiggleBone theJiggleBone = new jiggleBone();

                        //theJiggleBone.Name = bone_name;

                        using (var stream = File.Open(filepath, FileMode.Open))
                        {
                            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                            {
                                //read the name from the strings table
                                reader.BaseStream.Position = bone_name_offset;
                                theJiggleBone.Name = ReadNullTerminatedString(reader);


                                //set (and immediately make use of) the offset to this bones jigglebone data
                                theJiggleBone.Offset = jiggleboneDataOffset;
                                reader.BaseStream.Position = theJiggleBone.Offset;

                                //read flags into properties of theJiggleBone using a special function
                                theJiggleBone.getFlags(reader.ReadInt32());


                                //read everything else as a single (half a double)
                                theJiggleBone.length = reader.ReadSingle();

                                theJiggleBone.tipMass = reader.ReadSingle();


                                theJiggleBone.yawStiffness = reader.ReadSingle();

                                theJiggleBone.yawDamping = reader.ReadSingle();

                                theJiggleBone.pitchStiffness = reader.ReadSingle();

                                theJiggleBone.pitchDamping = reader.ReadSingle();

                                theJiggleBone.alongStiffness = reader.ReadSingle();

                                theJiggleBone.alongDamping = reader.ReadSingle();


                                theJiggleBone.angleLimit = reader.ReadSingle();


                                theJiggleBone.yawConstraintMax = reader.ReadSingle();

                                theJiggleBone.yawConstraintMax = reader.ReadSingle();

                                theJiggleBone.yawFriction = reader.ReadSingle();

                                theJiggleBone.yawBounce = reader.ReadSingle();


                                theJiggleBone.pitchConstraintMin = reader.ReadSingle();

                                theJiggleBone.pitchConstraintMax = reader.ReadSingle();

                                theJiggleBone.pitchFriction = reader.ReadSingle();

                                theJiggleBone.pitchBounce = reader.ReadSingle();


                                theJiggleBone.baseMass = reader.ReadSingle();

                                theJiggleBone.baseStiffness = reader.ReadSingle();

                                theJiggleBone.baseDamping = reader.ReadSingle();

                                theJiggleBone.baseLeftConstraintMin = reader.ReadSingle();

                                theJiggleBone.baseLeftConstraintMax = reader.ReadSingle();

                                theJiggleBone.baseLeftFriction = reader.ReadSingle();

                                theJiggleBone.baseUpConstraintMin = reader.ReadSingle();

                                theJiggleBone.baseUpConstraintMax = reader.ReadSingle();

                                theJiggleBone.baseUpFriction = reader.ReadSingle();

                                theJiggleBone.baseForwardConstraintMin = reader.ReadSingle();

                                theJiggleBone.baseForwardConstraintMax = reader.ReadSingle();

                                theJiggleBone.baseForwardFriction = reader.ReadSingle();


                                //todo: is_boing params. DO NOT READ if file wasn't compiled with isBoing or you'll read past the jigglebone data!

                                //todo: this is incorrect! there are 2 types of jigglebone and 2 subproperties!
                                if (theJiggleBone.isBoing)
                                {
                                    theJiggleBone.boingImpactSpeed = reader.ReadSingle();

                                    theJiggleBone.boingImpactAngle = reader.ReadSingle();


                                    theJiggleBone.boingDampingRate = reader.ReadSingle();

                                    theJiggleBone.boingFrequency = reader.ReadSingle();

                                    theJiggleBone.boingAmplitude = reader.ReadSingle();
                                }
                                else
                                //todo: need proper defaults for all values
                                {
                                    theJiggleBone.boingImpactSpeed = 0;

                                    theJiggleBone.boingImpactAngle = Convert.ToSingle(Math.Cos(0));


                                    theJiggleBone.boingDampingRate = 0;

                                    theJiggleBone.boingFrequency = 0;

                                    theJiggleBone.boingAmplitude = 0;
                                }


                                //add the jigglebone to the list of jigglebones, accessed by the Bone Name combobox. update combobox at same time so the indexes stay in sync
                                allJiggleBones.Add(theJiggleBone);
                                lstBoneName.Items.Add(theJiggleBone.Name);


                            }
                        }



                    }


                    //each bone is 216 bytes long. jump to the next bone.
                    bone_offset += 216;
                }

            }

            if (lstBoneName.Items.Count == 0)
            {
                this.Close();
                MessageBox.Show("No jigglebones found on model:" + "\n\n" + mdlpath);
                return;
            }

            lstBoneName.SelectedIndex = 0;

            this.Show();
        }



        [Serializable]
        public class jiggleBone
        {
            public int Offset { get; set; }

            public string Name { get; set; }

            //rotational property groups
            public bool isRigid { get; set; }
            public bool isFlexible { get; set; }

            //translational property groups
            public bool hasBaseSpring { get; set; }
            public bool isBoing { get; set; }




            public bool hasYawConstraint { get; set; }
            public bool hasPitchConstraint { get; set; }
            public bool hasAngleConstraint { get; set; }
            public bool hasLengthConstraint { get; set; }



            public Single length { get; set; }
            public Single tipMass { get; set; }

            public Single yawStiffness { get; set; }
            public Single yawDamping { get; set; }

            public Single pitchStiffness { get; set; }
            public Single pitchDamping { get; set; }

            public Single alongStiffness { get; set; }
            public Single alongDamping { get; set; }

            public Single angleLimit { get; set; }

            public Single yawConstraintMin { get; set; }
            public Single yawConstraintMax { get; set; }
            public Single yawFriction { get; set; } //todo: this ALSO seems to be unimplemented?? possibly hide both these unused things?? triple check!
            public Single yawBounce { get; set; } //pretty sure this is unimplemented, but store it anyway. maybe add a checkbox to show it

            public Single pitchConstraintMin { get; set; }
            public Single pitchConstraintMax { get; set; }
            public Single pitchFriction { get; set; } //todo: this ALSO seems to be unimplemented?? possibly hide both these unused things?? triple check!
            public Single pitchBounce { get; set; } //pretty sure this is unimplemented, but store it anyway. maybe add a checkbox to show it



            //has base spring!
            public Single baseMass { get; set; }

            public Single baseStiffness { get; set; }
            public Single baseDamping { get; set; }

            public Single baseLeftConstraintMin { get; set; }
            public Single baseLeftConstraintMax { get; set; }

            public Single baseLeftFriction { get; set; }

            public Single baseUpConstraintMin { get; set; }
            public Single baseUpConstraintMax { get; set; }

            public Single baseUpFriction { get; set; }

            public Single baseForwardConstraintMin { get; set; }
            public Single baseForwardConstraintMax { get; set; }

            public Single baseForwardFriction { get; set; }

            //NOTE: These fields seem to be only in models compiled with Source SDK Base 2013 MP and SP.
            //(might be important for a non-tf2 version of this?)

            //MORE IMPORTANT NOTE: If a model isn't compiled with boing jigglebones, it won't have the extra bytes required for those properties. so we are probably reading unknown data.

            public Single boingImpactSpeed { get; set; }
            public Single boingImpactAngle { get; set; }

            public Single boingDampingRate { get; set; }
            public Single boingFrequency { get; set; }
            public Single boingAmplitude { get; set; }


            //method to get all the jigglebone flags from the flags int
            public void getFlags(int jiggleFlags)
            {

                if ((jiggleFlags & JIGGLE_IS_FLEXIBLE) > 0)
                {
                    isRigid = false;
                    isFlexible = true;
                }
                else if ((jiggleFlags & JIGGLE_IS_RIGID) > 0)
                {
                    isRigid = true;
                    isFlexible = false;
                }
                //todo: this is incorrect! there are 2 types of jigglebone and 2 subproperties!
                if ((jiggleFlags & JIGGLE_HAS_BASE_SPRING) > 0)
                {
                    hasBaseSpring = false;
                    isBoing = true;
                }
                //probably a jigglebone that only has base spring or is boing
                else if ((jiggleFlags & JIGGLE_IS_BOING) > 0)
                {
                    hasBaseSpring = true;
                    isBoing = false;
                }


                if ((jiggleFlags & JIGGLE_HAS_YAW_CONSTRAINT) > 0)
                {
                    hasYawConstraint = true;
                }

                if ((jiggleFlags & JIGGLE_HAS_PITCH_CONSTRAINT) > 0)
                {
                    hasPitchConstraint = true;
                }

                if ((jiggleFlags & JIGGLE_HAS_ANGLE_CONSTRAINT) > 0)
                {
                    hasAngleConstraint = true;
                }

                if ((jiggleFlags & JIGGLE_HAS_LENGTH_CONSTRAINT) > 0)
                {
                    hasLengthConstraint = true;
                }

            }



            //funky extra properties for offset of this jigglebones start and end in the qc textbox
            public int QC_START { get; set; } = 0;

            public int QC_END { get; set; } = 0;



        }



        private void AddJiggleBone_Load(object sender, EventArgs e)
        {

        }

        private void lstBoneName_SelectedIndexChanged(object sender, EventArgs e)
        {
                                              
            jiggleBone theJiggleBone = allJiggleBones[lstBoneName.SelectedIndex];


            //JIGGLEBONE NAME
            //lstBoneName.Text = theJiggleBone.Name;


            //JIGGLE TYPE
            //lstJiggleType.SelectedIndex = Convert.ToInt32(theJiggleBone.jiggleType);
            if (theJiggleBone.isRigid)
            {
                chk_isRigid.Checked = true;
                chk_isFlexible.Checked = false;
            }
            else if (theJiggleBone.isFlexible)
            {
                chk_isFlexible.Checked = false;
                chk_isFlexible.Checked = true;
            }

            if (theJiggleBone.hasBaseSpring)
            {
                chk_hasBaseSpring.Checked = true;
                chk_isBoing.Checked = false;
            }
            else if(theJiggleBone.isBoing)
            {
                chk_hasBaseSpring.Checked = false;
                chk_isBoing.Checked = true;
            }



            //todo: this works but seems like a dumb way of locking the values?
            resetting_all_jigglebone_values = true;





            //GENERAL
            txtLength.SetNumber(theJiggleBone.length);
            txtTipMass.SetNumber(theJiggleBone.tipMass);

            chkAngleConstraint.Checked = theJiggleBone.hasAngleConstraint;
            txtAngleConstraint.SetNumber(theJiggleBone.angleLimit, true); // in radians

            //YAW
            txtYawStiffness.SetNumber(theJiggleBone.yawStiffness);
            txtYawDamping.SetNumber(theJiggleBone.yawDamping);

            chkYawConstraint.Checked = theJiggleBone.hasYawConstraint;
            txtYawConstraintMin.SetNumber(theJiggleBone.yawConstraintMin, true); // in radians
            txtYawConstraintMax.SetNumber(theJiggleBone.yawConstraintMax, true); // in radians

            //chkYawFriction.Checked = false;
            txtYawFriction.SetNumber(theJiggleBone.yawFriction);


            //PITCH
            txtPitchStiffness.SetNumber(theJiggleBone.pitchStiffness);
            txtPitchDamping.SetNumber(theJiggleBone.pitchDamping);

            chkPitchConstraint.Checked = theJiggleBone.hasPitchConstraint;
            txtPitchConstraintMin.SetNumber(theJiggleBone.pitchConstraintMin, true); // in radians
            txtPitchConstraintMax.SetNumber(theJiggleBone.pitchConstraintMax, true); // in radians

            //chkPitchFriction.Checked = false;
            txtPitchFriction.SetNumber(theJiggleBone.pitchFriction);


            //ALONG
            txtAlongStiffness.SetNumber(theJiggleBone.alongStiffness);
            txtAlongDamping.SetNumber(theJiggleBone.alongDamping);

            ///////////////////////////
            ///BASE SPRING PARAMS!!! //
            ///////////////////////////


            chkHasBaseSpring.Checked = theJiggleBone.hasBaseSpring;

            if (chkHasBaseSpring.Checked)
            {
                chkHasBaseSpring.Checked = theJiggleBone.hasBaseSpring;

                //GENERAL
                txtBaseStiffness.SetNumber(theJiggleBone.baseStiffness);
                txtBaseDamping.SetNumber(theJiggleBone.baseDamping);

                txtBaseBaseMass.SetNumber(theJiggleBone.baseMass);

                //LEFT_CONSTRAINT
                txtBaseLeftConstraintMin.SetNumber(theJiggleBone.baseLeftConstraintMin);
                txtBaseLeftConstraintMax.SetNumber(theJiggleBone.baseLeftConstraintMax);

                txtBaseLeftFriction.SetNumber(theJiggleBone.baseLeftFriction);

                //UP_CONSTRAINT
                txtBaseUpConstraintMin.SetNumber(theJiggleBone.baseUpConstraintMin);
                txtBaseUpConstraintMax.SetNumber(theJiggleBone.baseUpConstraintMax);

                txtBaseLeftFriction.SetNumber(theJiggleBone.baseLeftFriction);

                //FORWARD_CONSTRAINT
                txtBaseForwardConstraintMin.SetNumber(theJiggleBone.baseForwardConstraintMin);
                txtBaseForwardConstraintMax.SetNumber(theJiggleBone.baseForwardConstraintMax);

                txtBaseForwardFriction.SetNumber(theJiggleBone.baseForwardFriction);

            }


            ///////////////////////////
            ///   BOING PARAMS!!!   ///
            ///////////////////////////

            txtBoingImpactSpeed.SetNumber(theJiggleBone.boingImpactSpeed);
            txtBoingImpactAngle.SetNumber(theJiggleBone.boingImpactAngle, true, true); //run cosine on the degrees and then convert to radians

            txtBoingDampingRate.SetNumber(theJiggleBone.boingDampingRate);

            txtBoingFrequency.SetNumber(theJiggleBone.boingFrequency);
            txtBoingAmplitude.SetNumber(theJiggleBone.boingAmplitude);


            //done! let auto-refreshing commence.
            resetting_all_jigglebone_values = false;


        }


        private void jigglePropertyChanged(object sender, EventArgs e)
        {

            //if there are no bones loaded, don't do anything!
            if(lstBoneName.SelectedIndex == -1)
            {
                return;
            }

            //MessageBox.Show("if control equals controlname, set whichever relevant jigglebone property on the live model");

            // Properties ob =  (Object)sender;
            string obj_name = "";
            string arg = "";

            if (sender.GetType() == typeof(CheckBox))
            {
                CheckBox obj = (CheckBox)sender;
                obj_name = obj.Name.ToString();
                arg = obj.Checked.ToString();
            }

            if (sender.GetType() == typeof(TextBox))
            {
                TextBox obj = (TextBox)sender;
                obj_name = obj.Name.ToString();
                arg = obj.Text.ToString();
            }

            //todo: ugh...
            if (sender.GetType() == typeof(DeviceCtrlLibrary.NumericUpDown_CustomFormat))
            {
                DeviceCtrlLibrary.NumericUpDown_CustomFormat obj = (DeviceCtrlLibrary.NumericUpDown_CustomFormat)sender;
                obj_name = obj.Name.ToString();
                arg = obj.Text.ToString();
            }

            /*
            if (sender.GetType() == typeof(NumericUpDown))
            {
                NumericUpDown obj = (NumericUpDown)sender;
                obj_name = obj.Name.ToString();
                arg = obj.Text.ToString();
            }
            */

            jiggleBone theJiggleBone = allJiggleBones[lstBoneName.SelectedIndex];


            //todo: where is pitch/yaw friction and bounce? are you missing anything else??


            switch (obj_name)
            {
                //case 


                //GENERAL
                case "txtLength":
                    theJiggleBone.length = txtLength.GetNumber(false);
                    break;
                case "txtTipMass":
                    theJiggleBone.tipMass = txtTipMass.GetNumber(false);
                    break;

                //ANGLE
                case "chkAngleConstraint":
                    theJiggleBone.hasAngleConstraint = chkAngleConstraint.Checked;
                    break;
                case "txtAngleConstraint":
                    theJiggleBone.angleLimit = txtAngleConstraint.GetNumber(true); // in radians
                    break;

                //YAW
                case "txtYawStiffness":
                    theJiggleBone.yawStiffness = txtYawStiffness.GetNumber(false);
                    break;
                case "txtYawDamping":
                    theJiggleBone.yawDamping = txtYawDamping.GetNumber(false);
                    break;


                case "chkYawConstraint":
                    theJiggleBone.hasYawConstraint = chkYawConstraint.Checked;
                    break;

                case "txtYawConstraintMin":
                    theJiggleBone.yawConstraintMin = txtYawConstraintMin.GetNumber(true); // in radians
                    break;
                case "txtYawConstraintMax":
                    theJiggleBone.yawConstraintMax = txtYawConstraintMax.GetNumber(true); // in radians
                    break;

                //PITCH
                case "txtPitchStiffness":
                    theJiggleBone.pitchStiffness = txtPitchStiffness.GetNumber(false);
                    break;
                case "txtPitchDamping":
                    theJiggleBone.pitchDamping = txtPitchDamping.GetNumber(false);
                    break;


                case "chkPitchConstraint":
                    theJiggleBone.hasPitchConstraint = chkPitchConstraint.Checked;
                    break;

                case "txtPitchConstraintMin":
                    theJiggleBone.pitchConstraintMin = txtPitchConstraintMin.GetNumber(true); // in radians
                    break;
                case "txtPitchConstraintMax":
                    theJiggleBone.pitchConstraintMax = txtPitchConstraintMax.GetNumber(true); // in radians
                    break;


                //ALONG
                case "txtAlongStiffness":
                    theJiggleBone.alongStiffness = txtAlongStiffness.GetNumber(false);
                    break;
                case "txtAlongDamping":
                    theJiggleBone.alongDamping = txtAlongDamping.GetNumber(false);
                    break;


                ///////////////////////////
                ///BASE SPRING PARAMS!!! //
                ///////////////////////////
                case "chkHasBaseSpring":
                    theJiggleBone.hasBaseSpring = chkHasBaseSpring.Checked;

                    //disable all controls relating to base spring if it's unchecked
                    grp_HAS_BASE_SPRING.Enabled = chkHasBaseSpring.Checked;

                    break;

                //GENERAL
                case "txtBaseStiffness":
                    theJiggleBone.baseStiffness = txtBaseStiffness.GetNumber(false);
                    break;
                case "txtBaseDamping":
                    theJiggleBone.baseDamping = txtBaseDamping.GetNumber(false);
                    break;

                case "txtBaseBaseMass":
                    theJiggleBone.baseMass = txtBaseBaseMass.GetNumber(false);
                    break;

                //LEFT
                case "txtBaseLeftConstraintMin":
                    theJiggleBone.baseLeftConstraintMin = txtBaseLeftConstraintMin.GetNumber(false);
                    break;
                case "txtBaseLeftConstraintMax":
                    theJiggleBone.baseLeftConstraintMax = txtBaseLeftConstraintMax.GetNumber(false);
                    break;

                case "txtBaseLeftFriction":
                    theJiggleBone.baseLeftFriction = txtBaseLeftFriction.GetNumber(false);
                    break;

                //UP
                case "txtBaseUpConstraintMin":
                    theJiggleBone.baseUpConstraintMin = txtBaseUpConstraintMin.GetNumber(false);
                    break;
                case "txtBaseUpConstraintMax":
                    theJiggleBone.baseUpConstraintMax = txtBaseUpConstraintMax.GetNumber(false);
                    break;

                case "txtBaseUpFriction":
                    theJiggleBone.baseUpFriction = txtBaseUpFriction.GetNumber(false);
                    break;

                //FORWARD
                case "txtBaseForwardConstraintMin":
                    theJiggleBone.baseForwardConstraintMin = txtBaseForwardConstraintMin.GetNumber(false);
                    break;
                case "txtBaseForwardConstraintMax":
                    theJiggleBone.baseForwardConstraintMax = txtBaseForwardConstraintMax.GetNumber(false);
                    break;

                case "txtBaseForwardFriction":
                    theJiggleBone.baseForwardFriction = txtBaseForwardFriction.GetNumber(false);
                    break;

                ///////////////////////////
                ///   BOING PARAMS!!!   ///
                ///////////////////////////
                case "txtBoingImpactSpeed":
                    theJiggleBone.boingImpactSpeed = txtBoingImpactSpeed.GetNumber(false);
                    break;
                case "txtBoingImpactAngle":
                    theJiggleBone.boingImpactAngle = txtBoingImpactAngle.GetNumber(true, true);  //run radians to degrees on the inverse cosine of the bone
                    break;
                    
                case "txtBoingDampingRate":
                    theJiggleBone.boingDampingRate = txtBoingDampingRate.GetNumber(false);
                    break;

                case "txtBoingFrequency":
                    theJiggleBone.boingFrequency = txtBoingFrequency.GetNumber(false);
                    break;
                case "txtBoingAmplitude":
                    theJiggleBone.boingAmplitude = txtBoingAmplitude.GetNumber(false);
                    break;

            }


            if (chk_Auto_Apply.Checked && !resetting_all_jigglebone_values)
            {
                WriteJiggleBoneToFile();

                TFMV.Main.refresh_hlmv(false);

            }


            }





        private void WriteJiggleBoneConstraints(jiggleBone theJiggleBone)
        {

            //bit easier to read than \t \r\n repeatedly
            string Tab = "\t";
            string NewLine = "\r\n";


            string line = "";


            if (theJiggleBone.hasPitchConstraint)
            { 
                line = Tab;

                line += Tab;
                line += "pitch_constraint ";

                line += SingleToString(theJiggleBone.pitchConstraintMin, true);  // in radians

                line += " ";

                line += SingleToString(theJiggleBone.pitchConstraintMax, true);  // in radians


                line += NewLine;
                txt_QC.Text += line;

                line = Tab;

                line += Tab;
                line += "pitch_friction ";

                line += theJiggleBone.pitchFriction;

                line += NewLine;
                txt_QC.Text += line;

                line = Tab;

                line += Tab;
                line += "pitch_bounce ";

                line += theJiggleBone.pitchBounce;

                line += NewLine;
                txt_QC.Text += line;
            }

            if (theJiggleBone.hasYawConstraint)
            {
                line = Tab;

                line += Tab;
                line += "yaw_constraint ";

                line += SingleToString(theJiggleBone.yawConstraintMin);  // in radians

                line += " ";

                line += SingleToString(theJiggleBone.yawConstraintMax);  // in radians

                line += NewLine;
                txt_QC.Text += line;

                line = Tab;

                line += Tab;
                line += "yaw_friction ";

                line += theJiggleBone.yawFriction;

                line += NewLine;
                txt_QC.Text += line;

                line = Tab;

                line += Tab;
                line += "yaw_bounce ";

                line += theJiggleBone.yawBounce;

                line += NewLine;
                txt_QC.Text += line;
            }

            if (theJiggleBone.hasAngleConstraint)
                {
                line = Tab;

                line += Tab;
                line += "angle_constraint ";

                line += SingleToString(theJiggleBone.angleLimit, true); // in radians

                line += NewLine;
                txt_QC.Text += line;
            }
        }





        //todo: this should probably say jiggleBONES...?
        private void WriteJiggleBoneToFile()
        {






            //bit easier to read than \t \" \r\n repeatedly
            string Tab = "\t";
            string Quote = "\"";
            string NewLine = "\r\n";


            txt_QC.Text = "";


            //we can finally get qc params out of this!

            //iterate thru all jigglebones!!!
            for (var i = 0; i < allJiggleBones.Count; i++)
            {
                jiggleBone theJiggleBone = allJiggleBones[i];

                //remember where this bone starts so we can select it with the button
                theJiggleBone.QC_START = txt_QC.Text.Length;


                string line = "$jigglebone ";

                line += "\"";
                line += theJiggleBone.Name;
                line += Quote;

                line += NewLine;
                txt_QC.Text += line;

                line = "{";

                line += NewLine;
                txt_QC.Text += line;

                if (theJiggleBone.isFlexible)
                {
                    line = Tab;

                    line += "is_flexible";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += "{";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "length ";

                    line += theJiggleBone.length;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "tip_mass ";

                    line += theJiggleBone.tipMass;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "pitch_stiffness ";

                    line += theJiggleBone.pitchStiffness;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "pitch_damping ";

                    line += theJiggleBone.pitchDamping;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "yaw_stiffness ";

                    line += theJiggleBone.yawStiffness;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "yaw_damping ";

                    line += theJiggleBone.yawDamping;

                    line += NewLine;
                    txt_QC.Text += line;

                    if (!theJiggleBone.hasLengthConstraint)
                    {
                        line = Tab;

                        line += Tab;

                        line += "allow_length_flex";

                        line += NewLine;
                        txt_QC.Text += line;
                    }

                    line = Tab;

                    line += Tab;

                    line += "along_stiffness ";

                    line += theJiggleBone.alongStiffness;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "along_damping ";

                    line += theJiggleBone.alongDamping;

                    line += NewLine;
                    txt_QC.Text += line;

                    WriteJiggleBoneConstraints(theJiggleBone);


                    line = Tab;

                    line += "}";

                    line += NewLine;
                    txt_QC.Text += line;
                }

                if (theJiggleBone.isRigid) {
                    line = Tab;

                    line += "is_rigid";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += "{";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "length ";

                    line += theJiggleBone.length;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "tip_mass ";

                    line += theJiggleBone.tipMass;

                    line += NewLine;
                    txt_QC.Text += line;

                    WriteJiggleBoneConstraints(theJiggleBone);


                    line = Tab;

                    line += "}";

                    line += NewLine;
                    txt_QC.Text += line;
                }

                if (theJiggleBone.hasBaseSpring)
                {
                    line = Tab;

                    line += "has_base_spring";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += "{";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "base_mass ";

                    line += theJiggleBone.baseMass;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "stiffness ";

                    line += theJiggleBone.baseStiffness;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "damping ";

                    line += theJiggleBone.baseDamping;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "left_constraint ";

                    //old junk from Crowbar
                    //'line += MathModule.RadiansToDegrees(theJiggleBone.baseMinLeft);

                    //'line += " "

                    //'line += MathModule.RadiansToDegrees(theJiggleBone.baseMaxLeft);

                    //todo: so do these need to converted?
                    line += SingleToString(theJiggleBone.baseLeftConstraintMin, true);

                    line += " ";

                    line += SingleToString(theJiggleBone.baseLeftConstraintMax, true);

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "left_friction ";

                    line += SingleToString(theJiggleBone.baseLeftFriction);

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "up_constraint ";

                    //old junk from crowbar
                    //'line += MathModule.RadiansToDegrees(theJiggleBone.baseMinUp);

                    //'line += " "

                    //'line += MathModule.RadiansToDegrees(theJiggleBone.baseMaxUp);

                    line += SingleToString(theJiggleBone.baseUpConstraintMin, true);

                    line += " ";

                    line += SingleToString(theJiggleBone.baseUpConstraintMax, true);

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "up_friction ";

                    line += theJiggleBone.baseUpFriction;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "forward_constraint ";

                    //old junk from crowbar
                    //'line += MathModule.RadiansToDegrees(theJiggleBone.baseMinForward);

                    //'line += " "

                    //'line += MathModule.RadiansToDegrees(theJiggleBone.baseMaxForward);

                    line += SingleToString(theJiggleBone.baseForwardConstraintMin, true);

                    line += " ";

                    line += SingleToString(theJiggleBone.baseForwardConstraintMax, true);

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "forward_friction ";

                    line += SingleToString(theJiggleBone.baseForwardFriction);

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += "}";

                    line += NewLine;
                    txt_QC.Text += line;
                }

                if (theJiggleBone.isBoing)
                    {
                    line = Tab;

                    line += "is_boing";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += "{";

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "impact_speed ";

                    line += SingleToString(theJiggleBone.boingImpactSpeed);

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "impact_angle ";

                    line += SingleToString(theJiggleBone.boingImpactAngle, true, true); //run cosine on the degrees and then convert to radians

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "damping_rate ";

                    line += theJiggleBone.boingDampingRate;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "frequency ";

                    line += theJiggleBone.boingFrequency;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += Tab;

                    line += "amplitude ";

                    line += theJiggleBone.boingAmplitude;

                    line += NewLine;
                    txt_QC.Text += line;

                    line = Tab;

                    line += "}";

                    line += NewLine;
                    txt_QC.Text += line;
                }

                line = "}";

                line += NewLine;
                txt_QC.Text += line;


                //remember where this bone ends so we can select it with the button
                theJiggleBone.QC_END = txt_QC.Text.Length;




            }




            //string filepath = TFMV.Main.tfmv_dir + mdlpath;

            //todo: REMOVE THIS!
            string filepath = "C:\\Users\\jburn\\Documents\\Crowbar\\models\\jiggletest_135\\oktoberfester_jiggletest.mdl";

            //MessageBox.Show(filepath);

            //create the file if it doesn't exist before you write to it
            if (!File.Exists(filepath))
                {
                    File.WriteAllBytes(filepath, mdl_data);
                    //MessageBox.Show("wrote a file to " + filepath);
                }

                using (var stream = File.Open(filepath, FileMode.Open))
                {
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                    {

                    //iterate thru all jigglebones!!!
                    for (var i = 0; i < allJiggleBones.Count; i++)
                    {
                        jiggleBone theJiggleBone = allJiggleBones[i];



                        //seek to the start of the jigglebone data before you start writing
                        writer.BaseStream.Position = theJiggleBone.Offset;


                        //calculate the flags
                        int flags = 0;



                        if (theJiggleBone.isRigid)
                        {
                            flags = JIGGLE_IS_RIGID;
                        }
                        else if (theJiggleBone.isFlexible)
                        {
                            flags = JIGGLE_IS_FLEXIBLE;
                        }

                        if (theJiggleBone.isBoing)
                        {
                            flags = JIGGLE_IS_BOING;
                        }
                        else if (theJiggleBone.hasBaseSpring)
                        {
                            flags += JIGGLE_HAS_BASE_SPRING;
                        }

                        if (theJiggleBone.hasYawConstraint)
                        {
                            flags += JIGGLE_HAS_YAW_CONSTRAINT;
                        }

                        if (theJiggleBone.hasPitchConstraint)
                        {
                            flags += JIGGLE_HAS_PITCH_CONSTRAINT;
                        }

                        if (theJiggleBone.hasAngleConstraint)
                        {
                            flags += JIGGLE_HAS_ANGLE_CONSTRAINT;
                        }

                        if (theJiggleBone.hasLengthConstraint)
                        {
                            flags += JIGGLE_HAS_LENGTH_CONSTRAINT;
                        }

                        //write the flags
                        writer.Write(flags);

                        writer.Write(theJiggleBone.length);

                        writer.Write(theJiggleBone.tipMass);

                        writer.Write(theJiggleBone.yawStiffness);



                        writer.Write(theJiggleBone.yawDamping);

                        writer.Write(theJiggleBone.pitchStiffness);

                        writer.Write(theJiggleBone.pitchDamping);

                        writer.Write(theJiggleBone.alongStiffness);

                        writer.Write(theJiggleBone.alongDamping);


                        writer.Write(theJiggleBone.angleLimit);


                        writer.Write(theJiggleBone.yawConstraintMax);

                        writer.Write(theJiggleBone.yawConstraintMax);

                        writer.Write(theJiggleBone.yawFriction);

                        writer.Write(theJiggleBone.yawBounce);


                        writer.Write(theJiggleBone.pitchConstraintMin);

                        writer.Write(theJiggleBone.pitchConstraintMax);

                        writer.Write(theJiggleBone.pitchFriction);

                        writer.Write(theJiggleBone.pitchBounce);


                        writer.Write(theJiggleBone.baseMass);

                        writer.Write(theJiggleBone.baseStiffness);

                        writer.Write(theJiggleBone.baseDamping);


                        writer.Write(theJiggleBone.baseLeftConstraintMin);

                        writer.Write(theJiggleBone.baseLeftConstraintMax);

                        writer.Write(theJiggleBone.baseLeftFriction);


                        writer.Write(theJiggleBone.baseUpConstraintMin);

                        writer.Write(theJiggleBone.baseUpConstraintMax);

                        writer.Write(theJiggleBone.baseUpFriction);


                        writer.Write(theJiggleBone.baseForwardConstraintMin);

                        writer.Write(theJiggleBone.baseForwardConstraintMax);

                        writer.Write(theJiggleBone.baseForwardFriction);


                        writer.Write(theJiggleBone.boingImpactAngle);

                        writer.Write(theJiggleBone.boingImpactSpeed);


                        writer.Write(theJiggleBone.boingDampingRate);


                        writer.Write(theJiggleBone.boingFrequency);

                        writer.Write(theJiggleBone.boingAmplitude);


                    }

                }

            }

        }

        //applied jigglebones, show them in hlmv!
        private void btnApplyJigglebones_Click(object sender, EventArgs e)
        {
            WriteJiggleBoneToFile();
            TFMV.Main.refresh_hlmv(false);
        }

        private void lstJiggleType_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*
            //currently selected jigglebone
            jiggleBone theJiggleBone = allJiggleBones[lstBoneName.SelectedIndex];

            //enable these by default, isBoing overrides
            chkHasBaseSpring.Enabled = true;
            grpHasBaseSpring.Enabled = true;

            switch (lstJiggleType.SelectedIndex)
            {
                case 0: //none
                    theJiggleBone.jiggleType = JiggleType.none;
                    break;

                case 1: //is_flexible
                    theJiggleBone.jiggleType = JiggleType.isFlexible;
                break;

                case 2: //is_rigid
                    theJiggleBone.jiggleType = JiggleType.isRigid;
                    break;

                //todo: this is incorrect! there are 2 types of jigglebone and 2 subproperties!
                case 3: //is_boing
                    theJiggleBone.jiggleType = JiggleType.isBoing;
                    chkHasBaseSpring.Enabled = false;
                    grpHasBaseSpring.Enabled = false;
                    break;
            }
            */
        }




        private void evaluate_Jigglebone_Property_Groups()
        {
            //currently selected jigglebone
            jiggleBone theJiggleBone = allJiggleBones[lstBoneName.SelectedIndex];


            if (chk_isRigid.Checked)
            {
                theJiggleBone.isRigid = true;
                theJiggleBone.isFlexible = false;

                //todo: set up ui!
            }
            else if (chk_isFlexible.Checked)
            {
                theJiggleBone.isRigid = false;
                theJiggleBone.isFlexible = true;

                //todo: set up ui!
            }
            else
            {
                theJiggleBone.isRigid = false;
                theJiggleBone.isFlexible = false;

                //todo: set up ui!
            }



            if (chk_hasBaseSpring.Checked)
            {
                theJiggleBone.hasBaseSpring = true;
                theJiggleBone.isBoing = false;

                //todo: set up ui!
            }
            else if (chk_isBoing.Checked)
            {
                theJiggleBone.hasBaseSpring = false;
                theJiggleBone.isBoing = true;

                //todo: set up ui!
            }
            else
            {
                theJiggleBone.hasBaseSpring = false;
                theJiggleBone.isBoing = false;

                //todo: set up ui!
            }


        }



        //handle property groups so you can't select any that don't work together
        private void chk_isRigid_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_isRigid.Checked)
            {
                chk_isFlexible.Checked = false;

                lbl_IS_FLEXIBLE.Text = "IS_RIGID";

                //allow length flex
                chkAllowLengthFlex.Visible = false;

                //pitch
                btnPitchStiffness.Visible = false;
                lblPitchStiffness.Visible = false;
                txtPitchStiffness.Visible = false;

                btnPitchDamping.Visible = false;
                lblPitchDamping.Visible = false;
                txtPitchDamping.Visible = false;

                //yaw
                btnYawStiffness.Visible = false;
                lblYawStiffness.Visible = false;
                txtYawStiffness.Visible = false;

                btnYawDamping.Visible = false;
                lblYawDamping.Visible = false;
                txtYawDamping.Visible = false;

                //along
                lbl_Along.Visible = false;
                grp_Along.Visible = false;

                //make sure we can see it
                grp_IS_FLEXIBLE.Location = LEFT_PROPERTY_PANEL_LOCATION;


                //chk_isFlexible.Enabled = false;
            }
            else
            {
                //only hide this panel if both variants of it aren't being used
                if (!chk_isFlexible.Checked)
                {
                    grp_IS_FLEXIBLE.Location = NULL_PROPERTY_PANEL_LOCATION;
                }

                //chk_isFlexible.Enabled = true;

            }

            evaluate_Jigglebone_Property_Groups();
        }

        private void chk_isFlexible_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_isFlexible.Checked)
            {
                chk_isRigid.Checked = false;

                lbl_IS_FLEXIBLE.Text = "IS_FLEXIBLE";

                //allow length flex
                chkAllowLengthFlex.Visible = true;

                //pitch
                btnPitchStiffness.Visible = true;
                lblPitchStiffness.Visible = true;
                txtPitchStiffness.Visible = true;

                btnPitchDamping.Visible = true;
                lblPitchDamping.Visible = true;
                txtPitchDamping.Visible = true;

                //yaw
                btnYawStiffness.Visible = true;
                lblYawStiffness.Visible = true;
                txtYawStiffness.Visible = true;

                btnYawDamping.Visible = true;
                lblYawDamping.Visible = true;
                txtYawDamping.Visible = true;

                //along
                lbl_Along.Visible = true;
                grp_Along.Visible = true;

                //make sure we can see it
                grp_IS_FLEXIBLE.Location = LEFT_PROPERTY_PANEL_LOCATION;

                //chk_isRigid.Enabled = false;
            }
            else
            {
                //only hide this panel if both variants of it aren't being used
                if (!chk_isRigid.Checked)
                {
                    grp_IS_FLEXIBLE.Location = NULL_PROPERTY_PANEL_LOCATION;
                }

                //chk_isRigid.Enabled = true;
            }

            evaluate_Jigglebone_Property_Groups();
        }

        private void chk_hasBaseSpring_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_hasBaseSpring.Checked)
            {
                chk_isBoing.Checked = false;

                grp_HAS_BASE_SPRING.Location = RIGHT_PROPERTY_PANEL_LOCATION;
                //chk_isBoing.Enabled = false;
            }
            else
            {
                grp_HAS_BASE_SPRING.Location = NULL_PROPERTY_PANEL_LOCATION;
                //chk_isBoing.Enabled = true;
            }

            evaluate_Jigglebone_Property_Groups();
        }

        private void chk_isBoing_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_isBoing.Checked)
            {
                chk_hasBaseSpring.Checked = false;

                grp_IS_BOING.Location = RIGHT_PROPERTY_PANEL_LOCATION;
                //chk_hasBaseSpring.Enabled = false;
            }
            else
            {
                grp_IS_BOING.Location = NULL_PROPERTY_PANEL_LOCATION;
                //chk_hasBaseSpring.Enabled = true;
            }

            evaluate_Jigglebone_Property_Groups();
        }


        private string param_to_reset = "";

        private void ResetParamToDefault(string Param_To_Reset)
        {
            MessageBox.Show("todo: finish this reset function. each reset button must be linked to a textbox and each textbox must be linked to a param.");
            //Param_To
        }


        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
        }

        private void btn_reset_generic_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;

            param_to_reset = button.Tag.ToString();

            menuResetValue.Show(MousePosition);
        }




        private void resetToDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetParamToDefault(param_to_reset);
        }

        private void revertToOriginalboneNameValueToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void groupBox21_Enter(object sender, EventArgs e)
        {

        }

        private void button26_Click(object sender, EventArgs e)
        {

        }

        private void chk_Always_On_Top_CheckedChanged(object sender, EventArgs e)
        {
            
            if (chk_Always_On_Top.Checked)
            {
                //this.TopMost = true;

                //always_on_top_thread.Resume();

            }
            else
            {
                //always_on_top_thread.Suspend();
                this.TopMost = false;
            }
            


        }

        private string txt_QC_backup = "";

        private void txt_QC_Enter(object sender, EventArgs e)
        {
            txt_QC_backup = txt_QC.Text;
        }

        private void txt_QC_TextChanged(object sender, EventArgs e)
        {
            return;
            //todo: idk?
            bool show_error = (txt_QC.Text == txt_QC_backup);
            txt_QC.Text = txt_QC_backup;
            if (show_error)
            {
                //MessageBox.Show("This text is not directly editable in the BETA version.\n\nPlease use the controls on the left.", "Sorry!");
            }
        }


        private void numericUpDown_any__ValueChanged(object sender, EventArgs e)
        {

        }



        //for converting any of the jigglebone properties to their correct string equivalent (with option to convert radians to degrees)
        public static string SingleToString(double value, bool RadiansToDegrees = false, bool isBoingImpactAngle = false)
        {

            //according to the crowbar source, we need the INVERSE cosine of the actual value
            //this is ONLY for boingImpactAngle
            if (isBoingImpactAngle)
            {
                value = Math.Acos(value);
            }

            //conversion function from crowbar
            if (RadiansToDegrees)
            {
                value = value * 180 / 3.1415926535897931;

                //value = Math.Round(value);

                /*
                //deal with numbers that aren't quite correct as a result of conversion
                double rounded = Math.Floor(value);
                if (value - rounded >= 0.99999)
                {
                    value = rounded + 1;
                }
                */
            }


            string value_formatted = value.ToString("0.#####");

            /*
            //set correct string length for desired decimal places, accounting for negative numbers
            int StringLength = 8;
            if (value < 0)
            {
                StringLength = 9;
            }


            //trim string down to appropriate length, don't trim if it's not long enough
            if (value_formatted.Length >= StringLength)
            {
                value_formatted = value_formatted.Substring(0, StringLength);
            }

            value_formatted = value_formatted.Replace(".000000", "");
            */

            return value_formatted;
        }

        private void btn_QC_select_current_Click(object sender, EventArgs e)
        {
            jiggleBone theJiggleBone = allJiggleBones[lstBoneName.SelectedIndex];

            int QC_start = theJiggleBone.QC_START;
            int QC_count = (theJiggleBone.QC_START - theJiggleBone.QC_END);

            if (QC_count < 1 || (QC_start + QC_count) > txt_QC.TextLength)
            {
                theJiggleBone.QC_START = 0;
                theJiggleBone.QC_END = 0;
                return;
            }

            txt_QC.Select(QC_start, QC_count);
            txt_QC.Focus();

        }

        private void btn_QC_select_all_Click(object sender, EventArgs e)
        {
            txt_QC.Select(0, txt_QC.TextLength);
            txt_QC.Focus();
        }


        private void btn_QC_copy_Click(object sender, EventArgs e)
        {

            System.Windows.Forms.Clipboard.SetText(txt_QC.Text.Substring(txt_QC.SelectionStart, txt_QC.SelectionLength));
        }


        private void numericUpDown_any_ValueChanged(object sender, EventArgs e)
        {
            //DeviceCtrlLibrary.NumericUpDown_CustomFormat i guess
            //NumericUpDown numericUpDown = sender as NumericUpDown;


            /*
            if (numericUpDown.Value != 0)
            {
                numericUpDown.Value = 0;
            }
            */
        }

    }




    namespace ExtensionMethods
    {
        public static class MyExtensions
        {

            //extends NumericUpDown, puts a float into the text box and correctly formats it (with option to convert radians to degrees)
            public static void SetNumber(this System.Windows.Forms.NumericUpDown txtBox, double value, bool RadiansToDegrees = false, bool isBoingImpactAngle = false)
            {
                txtBox.Text = AddJiggleBone.SingleToString(value, RadiansToDegrees, isBoingImpactAngle);
                return;
            }

        //extends NumericUpDown, gets the text from the text box and correctly formats it as a Single (with option to convert degrees to radians)
        public static Single GetNumber(this System.Windows.Forms.NumericUpDown txtBox, bool DegreesToRadians, bool isBoingImpactAngle = false)
            {


                if (!double.TryParse(txtBox.Text, out double number))
                    {

                    //todo: could reset to default instead
                    txtBox.Text = "0";

                }


                double value = Convert.ToDouble(txtBox.Text);


                //conversion function from crowbar
                if (DegreesToRadians)
                {
                    value = value * 3.1415926535897931 / 180;
                }

                //according to the crowbar source, we need the cosine of the actual value
                //this is ONLY for boingImpactAngle
                if (isBoingImpactAngle)
                {
                    value = Math.Cos(value);
                }



                return Convert.ToSingle(value);
            }




        }

}
}
