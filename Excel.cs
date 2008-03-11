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
	public class ElExcel
	{
		private static Excel.Application ApExcel;
		public ElExcel()
		{
		}
		public static Excel.Application CrearArchivo(string nombreArchivo){
			if(ApExcel==null) ApExcel = new Excel.Application();
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
			             Excel.XlSaveAsAccessMode.xlNoChange,opc,opc,opc,opc,opc);
			libro.Close(opc,opc,opc);
			return ApExcel;
		}
	}
	[TestFixture]
	public class probarElExcel{
		[Test]
		public void CrearArchivo(){
			ElExcel.CrearArchivo("borrar_prueba.xls");
		}
	}
}
