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
			l.Name="nombre";
			l.Text="texto";
			f.Controls.Add(l);
			Application.Run(f);
		}
	}
}
