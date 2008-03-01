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
					// System.Console.WriteLine(": "+o.GetType().Name);
				}
			}
		}
	}
}
