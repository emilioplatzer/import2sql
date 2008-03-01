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

namespace TodoASql
{
	/// <summary>
	/// Description of Formularios.
	/// </summary>
	public class Formulario:Form
	{
		bool camposAgregados=false;
		public Formulario()
		{
		}
		public void AgregarCampos(){
			if(!camposAgregados){
				camposAgregados=true;
				FieldInfo[] fs=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				foreach(FieldInfo f in fs){
					Object o=f.GetValue(this);
					System.Console.Write("campo "+f.Name);
					if(o!=null){
						System.Console.Write(" valor "+o.ToString());
						if(o.GetType().IsSubclassOf(typeof(System.Windows.Forms.Control))){
							System.Console.Write(" se puede castear");
							Control control=(Control) o;
							Controls.Add(control);
						}else{
							TypeConverter conv=TypeDescriptor.GetConverter(f.FieldType);
							if(conv.CanConvertTo(typeof(System.Windows.Forms.Control))){
								System.Console.Write(" se puede convertir");
								Control control=(Control) conv.ConvertTo(o,typeof(System.Windows.Forms.Control));
								Controls.Add(control);
							}
						}
						// System.Console.WriteLine(": "+o.GetType().Name);
					}
					System.Console.WriteLine();
				}
			}
		}
	}
}
