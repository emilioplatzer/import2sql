/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 01/03/2008
 * Time: 10:49 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of Formularios.
	/// </summary>
	public class Formulario:Form
	{
		bool camposAgregados=false;
		Object ObjetoBase;
		public Formulario()
		{
		}
		public void AgregarCampos(){
			if(!camposAgregados){
				camposAgregados=true;
				FieldInfo[] fs=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				foreach(FieldInfo f in fs){
					Object o=f.GetValue(this);
					if(o!=null){
						if(o.GetType().IsSubclassOf(typeof(System.Windows.Forms.Control))){
							Control control=(Control) o;
							Controls.Add(control);
						}
					}
				}
			}
		}
		public void GenerarDesdeObjeto(Object objeto){
			Assert.IsNotNull(objeto);
			ObjetoBase=objeto;
			int xlbl=10, y=10, xtxt=140;
			FieldInfo[] fs=ObjetoBase.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo f in fs){
				Object o=f.GetValue(ObjetoBase);
				TypeConverter conv=TypeDescriptor.GetConverter(f.FieldType);
				if(conv.CanConvertFrom(typeof(string))
				  & conv.CanConvertTo(typeof(string)))
				{
					string objetoValor=(string) conv.ConvertTo(o,typeof(string));
					Label l=new Label();
					l.Name="lbl_"+f.Name;
					l.Text=f.Name;
					l.Left=xlbl;
					l.Top=y;
					Controls.Add(l);
					TextBox t=new TextBox();
					t.Name="txt_"+f.Name;
					t.Text=objetoValor;
					t.Left=xtxt;
					t.Top=y;
					Controls.Add(t);
					y+=l.Height*5/4;
				}
			}
			Button b=new Button();
			b.Name="btn_Enter";
			b.Text="Tomar";
			b.Left=xtxt;
			b.Top=y;
			b.Click+= new EventHandler(EventoBotonTomarDesdeObjeto);
			Controls.Add(b);
		}
		public void VolverAlObjeto(){
			Assert.IsNotNull(ObjetoBase);
			FieldInfo[] fs=ObjetoBase.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo f in fs){
				TypeConverter conv=TypeDescriptor.GetConverter(f.FieldType);
				if(conv.CanConvertFrom(typeof(string))
				  & conv.CanConvertTo(typeof(string)))
				{
					string valor=Controls["txt_"+f.Name].Text;
					Object objetoValor=conv.ConvertFrom(valor);
					f.SetValue(ObjetoBase,objetoValor);
				}
			}
		}
		private void EventoBotonTomarDesdeObjeto(object sender, System.EventArgs e){
			VolverAlObjeto();
			System.Console.WriteLine("par.Frase "+ObjetoBase.ToString());
			Close();
		}
	}
	[TestFixture]
	public class ProbarFormulario{
		[Test]
		public void FormDerivado(){
			PruebaFormDerivado form=new PruebaFormDerivado();
			form.Show();
			Assert.IsNotNull(form,"el form está abierto");
			Assert.IsTrue(form.Visible,"el form es visible");
			Assert.AreEqual("valor inicial",form.Controls["controlTextBox"].Text);
			form.CambiarAlgunosValores();
			Assert.AreEqual("valor cambiado",form.Controls["controlTextBox"].Text);
			Button b=(Button) form.Controls["botonCerrar"];
			Assert.Ignore("Falta apretar el botón y ver si cierra");
		}
	}
	public class PruebaFormDerivado:Formulario{
		Label lbl=new Label();
		TextBox t;
		Button b;
		public PruebaFormDerivado(){
			lbl.Text="La etiqueta";
			lbl.Left=lbl.Height/4;
			lbl.Top=lbl.Height/4;
			t=new TextBox();
			t.Text="valor inicial";
			t.Name="controlTextBox";
			t.Left=lbl.Left;
			t.Top=lbl.Bottom+lbl.Height/4;
			b=new Button();
			b.Text="aceptar";
			b.Name="botonCerrar";
			b.Left=lbl.Left;
			b.Top=t.Bottom+lbl.Height/4;
			b.Click+= delegate(object sender, EventArgs e) { 
				this.Close();
			};
			AgregarCampos();
			//
			/*
			Controls.Add(t);
			Controls.Add(lbl);
			// */
		}
		public void CambiarAlgunosValores(){
			t.Text="valor cambiado";
		}
	}
}
