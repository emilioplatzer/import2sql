/*
 * Creado por SharpDevelop.
 * Usuario: eplatzer
 * Fecha: 05/06/2008
 * Hora: 12:18
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Drawing;
using System.Windows.Forms;

using Comunes;
using BasesDatos;
using Interactivo;

namespace Tareas
{
	public class RezonificacionDomiciliaria:Formulario{
		public RezonificacionDomiciliaria(){
			Dibujar();
		}
		public void AgregarOpcion(string titulo, Accion accion, Accion ver, string explicacion){
			AgregarGrillado(titulo,accion);
			AgregarGrillado("ver",ver);
			AgregarGrillado(explicacion);
			ProximaLinea();
		}
		public void Dibujar(){
			PonerTamannoAplicacion();
			AgregarLogo(new Bitmap("GifForm.gif"),0);
			AgregarTitulo("Cambio de secciones/comunas");
			AgregarOpcion("Importar",ImportarCalles,VerCallesImportadas,"Importa la nueva guía de calles");
			AgregarOpcion("Concordar",ReasignarCalles,VerCallesReasignadas,"Relaciona los nombres de las calles del padrón con las calles de la guía");
			AgregarOpcion("Seccionar",ReasignarComunas,VerComunasReasignadas,"Asigna comuna y circuito en base a la nueva guía de calles");
		}
		public static void Run(){
			Application.Run(new RezonificacionDomiciliaria());
		}
		public void ImportarCalles(){
			
		}
		public void VerCallesImportadas(){
			
		}
		public void ReasignarCalles(){
			
		}
		public void VerCallesReasignadas(){
			
		}
		public void ReasignarComunas(){
			
		}
		public void VerComunasReasignadas(){
			
		}
	}
}
