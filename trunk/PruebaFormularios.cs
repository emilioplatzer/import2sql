/*
 * Creado por SharpDevelop.
 * Usuario: eplatzer
 * Fecha: 29/02/2008
 * Hora: 13:13
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Windows;
using System.Windows.Forms;

namespace TodoASql
{
	/// <summary>
	/// Description of PruebaFormularios.
	/// </summary>
	public class PruebaFormularios
	{
		public PruebaFormularios()
		{
		}
		public static void Primero()
		{
			Form f=new Form();
			Label l=new Label();
			l.Name="etiqueta_l";
			l.Text="texto de la etiqueta";
			f.Controls.Add(l);
			TextBox t=new TextBox();
			t.Name="textbox_t";
			t.Text="el texto a editar";
			t.Top=l.Bottom+l.Height/8;
			f.Controls.Add(t);
			Button b=new Button();
			b.Text="Entrar";
			b.Top=t.Bottom+l.Height/8;
			f.Controls.Add(b);
			b.Click+= delegate(object sender, EventArgs e) { 
				PruebaFormDerivado form=new PruebaFormDerivado();
				form.Show();
				form.CambiarAlgunosValores();
				Formulario f2=new Formulario();
				ParametrosPrueba par=new ParametrosPrueba(ParametrosPrueba.LeerPorDefecto.SI);
				f2.GenerarDesdeObjeto(par);
				f2.Show();
				System.Windows.Forms.MessageBox.Show("Ya lo mostré");
			};
			Application.Run(f);
		}
	}
}
