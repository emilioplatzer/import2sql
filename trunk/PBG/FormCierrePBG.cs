/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 28/05/2008
 * Hora: 07:05 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using Comunes;
using Interactivo;

namespace PBG
{
	public class FormCierrePBG:Formulario{
		public FormCierrePBG(){
			Dibujar();
		}
		public void Dibujar(){
			System.Windows.Forms.PictureBox pBox=new PictureBox();
			// pBox.SizeMode=PictureBoxSizeMode.StretchImage;
			// pBox.SizeMode=PictureBoxSizeMode.AutoSize;
			pBox.SizeMode=PictureBoxSizeMode.Zoom;
			Image iPrincipal=new Bitmap(@"e:\hecho\cs\import2sql\bin\Debug\PBG.jpg");
			BackgroundImage=iPrincipal;
			BackgroundImageLayout=ImageLayout.Stretch;
			BackgroundImageLayout=ImageLayout.Zoom;
			pBox.Height=350;
			pBox.Width=pBox.Height*845/1011;
			pBox.Image=iPrincipal;
			// AgregarEnLinea(pBox);
			Label titulo=new Label();
			titulo.Text="CierrePBG";
			titulo.Font=new Font("Arial",40,FontStyle.Bold);
			titulo.AutoSize=true;
			titulo.BackColor=Color.Transparent;
			AgregarEnLinea(titulo);
		}
	}
}
