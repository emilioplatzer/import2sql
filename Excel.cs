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
	public class LibroExcel:HojaExcel //:AccesoExcel
	{
		Excel.Workbook libro;
		static object ___ = Type.Missing; 		
		private static Excel.Application apExcel;
		public static Excel.Application ApExcel{
			get{
				if(apExcel==null) apExcel = new Excel.Application();
				apExcel.Visible=true;
				return apExcel;
			}
		}
		private LibroExcel(Excel.Workbook libro)
			:base((Excel.Worksheet) libro.Worksheets[1])
		{
			this.libro=libro;
		}
		static LibroExcel()
		{
			AppDomain.CurrentDomain.DomainUnload+= delegate { 
				if(apExcel!=null){
					apExcel.Quit();
					apExcel=null;
				}
			};
		}
		public static LibroExcel Abrir(string nombreArchivo){
			return new LibroExcel(ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___));
		}
		public static LibroExcel Nuevo(){
			return new LibroExcel(ApExcel.Workbooks.Add(___));
		}
		public HojaExcel Hoja(string etiqueta){
			return new HojaExcel((Excel.Worksheet) libro.Worksheets[etiqueta]);
		}
		public void GuardarYCerrar(string nombreArchivo){
			libro.Close(true,nombreArchivo,___);
		}
		public void Close(){
			libro.Close(___,___,___);
		}
	}
	public class HojaExcel:AccesoExcel
	{
		Excel.Worksheet hoja;
		static object ___ = Type.Missing; 		
		public HojaExcel(Excel.Worksheet hoja)
			:base(hoja)
		{
			this.hoja=hoja;
		}
		public RangoExcel Rango(string esquina,string otraEsquina){
			return new RangoExcel(hoja.get_Range(esquina,otraEsquina));
		}
	}
	public class RangoExcel:AccesoExcel{
		Excel.Range Rango;
		static object ___ = Type.Missing; 		
		internal RangoExcel(Excel.Range rango)
			:base(rango)
		{
			this.Rango=rango;
		}
		public int CantidadFilas{
			get{ return Rango.Rows.Count;}
		}
		public int CantidadColumnas{
			get{ return Rango.Columns.Count;}
		}
	}
	public class AccesoExcel{
		Excel.Range Base;
		static object ___ = Type.Missing; 		
		protected AccesoExcel(Excel.Range Base){
			this.Base=Base;
		}
		protected AccesoExcel(Excel.Worksheet hoja){
			this.Base=hoja.get_Range("A1",___);
		}
		public string TextoCelda(string rango){
			return Base.get_Range(rango,___).Text.ToString();
		}
		public string TextoCelda(int fila, int col){
			return ((Excel.Range) Base.Cells[fila,col]).Text.ToString();
		}
		public void PonerTexto(int fila,int col,string valor){
			((Excel.Range) Base.Cells[fila,col]).Value2=valor;
		}
		public void Rellenar(string[,] matriz){
			for(int fila=0;fila<matriz.GetLength(0);fila++){
				for(int col=0;col<matriz.GetLength(1);col++){
					PonerTexto(fila+1,col+1,matriz[fila,col]);
				}
			}
		}
	}
	[TestFixture]
	public class probarLibroExcel{
		string nombreArchivo=Archivo.CarpetaActual()+"\\borrar_prueba.xls";
		public probarLibroExcel(){
		}
		[Test]
		public void SegundoLeerAlgunosDatos(){
			LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
			HojaExcel hoja=libro.Hoja("LaHoja1");
			Assert.AreEqual("uno",hoja.TextoCelda("A1"));
			Assert.AreEqual("dos",hoja.TextoCelda(2,2));
			Assert.AreEqual("pi",hoja.TextoCelda("N3"));
			libro.Close();
		}
		[Test]
		public void Rango(){
			LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
			HojaExcel hoja=libro.Hoja("LaHoja1");
			RangoExcel rango=hoja.Rango("B2","N3");
			Assert.AreEqual(2,rango.CantidadFilas);
			Assert.AreEqual(13,rango.CantidadColumnas);
			Assert.AreEqual("dos",rango.TextoCelda("A1"));
			Assert.AreEqual("dos",hoja.TextoCelda("B2"));
			Assert.AreEqual("dos",libro.TextoCelda("B2"));
			libro.Close();
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
			// libro = ApExcel.Workbooks.Add(___);
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
		public void PrimeroMeterAlgunosDatos(){
			Excel.Workbook libro=ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___);
			Excel.Worksheet hoja=(Excel.Worksheet) libro.Worksheets["LaHoja1"];
			hoja.Cells[1,1]="uno";
			hoja.Cells[2,2]="dos";
			hoja.Cells[3,14]="pi";
			libro.Save();
			libro.Close(___,___,___);
		}
		[Test]
		public void SegundoLeerAlgunosDatos(){
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
