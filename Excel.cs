/*
 * Creado por SharpDevelop.
 * Usuario: Andrea
 * Fecha: 11/03/2008
 * Hora: 20:47
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using Excel = Microsoft.Office.Interop.Excel;
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of Excel.
	/// </summary>
	public class UnExcel
	{
		private static Excel.Application ApExcel;
		private UnExcel(){
			
		}
		static UnExcel()
		{
			AppDomain.CurrentDomain.DomainUnload+= delegate { 
				ApExcel.Quit();
				ApExcel=null;
			};
		}
		public static Excel.Workbook Nuevo(string nombreArchivo){
			if(ApExcel==null) ApExcel = new Excel.Application();
			Archivo.Borrar(nombreArchivo);
			Assert.IsFalse(Archivo.Existe(nombreArchivo));
			object opc = Type.Missing;
			Excel.Workbook libro;
			libro = ApExcel.Workbooks.Add(opc);
			ApExcel.Visible = true;
			libro = ApExcel.Workbooks.Add(opc);
			Excel.Worksheet hoja1 = new Excel.Worksheet();
			hoja1 = (Excel.Worksheet)libro.Sheets.Add(opc, opc, opc, opc);
			hoja1.Activate();
			hoja1.Name="LaHoja1";
			libro.SaveAs(nombreArchivo,opc,opc,opc,opc,opc,
			             Excel.XlSaveAsAccessMode.xlNoChange,
			             Excel.XlSaveConflictResolution.xlLocalSessionChanges,
			             opc,opc,opc,opc);
			libro.Close(opc,opc,opc);
			return libro;
		}
	}
	[TestFixture]
	public class probarElExcel{
		[Test]
		public void CrearArchivo(){
			string archivo=Archivo.CarpetaActual()+"\\borrar_prueba.xls";
			Excel.Workbook libro=UnExcel.CrearArchivo(archivo);
			Assert.IsTrue(Archivo.Existe(archivo));
			Excel.Worksheet hoja=(Excel.Worksheet) libro.Worksheets["LaHoja1"];
			// Excel.Worksheet hoja=libro.Worksheets.Item("LaHoja1");  // ["LaHoja1"];
			hoja.Cells[1,1]="uno";
			hoja.Cells[2,2]="dos";
			libro.Save();
		}
	}
}
