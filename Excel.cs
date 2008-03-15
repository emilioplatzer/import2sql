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
		private static Excel.Application apExcel;
		public static Excel.Application ApExcel{
			get{
				if(apExcel==null) apExcel = new Excel.Application();
				return apExcel;
			}
		}
		private UnExcel(){
			
		}
		static UnExcel()
		{
			AppDomain.CurrentDomain.DomainUnload+= delegate { 
				if(apExcel!=null){
					apExcel.Quit();
					apExcel=null;
				}
			};
		}
	}
	[TestFixture]
	public class probarExcel{
		Excel.Application ApExcel;
		object ___ = Type.Missing; 
		string nombreArchivo=Archivo.CarpetaActual()+"\\borrar_prueba.xls";
		public probarExcel(){
			ApExcel = new Excel.Application();
		}
		~probarExcel(){
			ApExcel.Quit();
		}
		[Test]
		public void CrearArchivo(){
			Archivo.Borrar(nombreArchivo);
			Assert.IsFalse(Archivo.Existe(nombreArchivo));
			Excel.Workbook libro;
			libro = ApExcel.Workbooks.Add(___);
			ApExcel.Visible = true;
			libro = ApExcel.Workbooks.Add(___);
			Excel.Worksheet hoja1 = new Excel.Worksheet();
			hoja1 = (Excel.Worksheet)libro.Sheets.Add(___, ___, ___, ___);
			hoja1.Activate();
			hoja1.Name="LaHoja1";
			libro.SaveAs(nombreArchivo,___,___,___,___,___,
			             Excel.XlSaveAsAccessMode.xlNoChange,
			             Excel.XlSaveConflictResolution.xlLocalSessionChanges,
			             ___,___,___,___);
			libro.Close(___,___,___);
			Assert.IsTrue(Archivo.Existe(nombreArchivo));
		}
		[Test]
		public void MeterAlgunosDatos(){
			Excel.Workbook libro=ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___);
			Excel.Worksheet hoja=(Excel.Worksheet) libro.Worksheets["LaHoja1"];
			hoja.Cells[1,1]="uno";
			hoja.Cells[2,2]="dos";
			hoja.Cells[3,14]="pi";
			libro.Save();
		}
		[Test]
		public void LeerAlgunosDatos(){
			Excel.Workbook libro=ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___);
			Excel.Worksheet hoja=(Excel.Worksheet) libro.Worksheets["LaHoja1"];
			// Excel.Range r=hoja.Range["A1"];
			Excel.Range r;
			r=hoja.get_Range("A1",___);
			Assert.AreEqual("uno",r.Text.ToString());
			Assert.AreEqual("dos",((Excel.Range) hoja.Cells[2,2]).Text);
			Assert.AreEqual("pi",hoja.get_Range("N3",___).Text);
		}
	}
}
