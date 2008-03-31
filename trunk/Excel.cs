/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 11/03/2008
 * Hora: 20:47
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
#if SinOffice
#else
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
		internal static string FormatoFechas=null; //null=autodetectar "dd/mm/aaaa";
		bool abierto=false; public bool Abierto{ get{ return abierto; }}
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
			LibroExcel nuevo=new LibroExcel(ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___));
			nuevo.abierto=true;
			return nuevo;
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
		public void DescartarYCerrar(){
			libro.Saved=true;
			libro.Close(false,___,___);
		}
		public void GuardarYCerrar(){
			libro.Close(true,___,___);
		}
		public void CerrarNoHayCambios(){
			Assert.IsTrue(libro.Saved,"Error hay cambios inesperados en la hoja de datos "+libro.Name);
			libro.Close(___,___,___);
			abierto=false;
		}
	}
	public class HojaExcel:AccesoExcel{
		static object ___ = Type.Missing; 		
		public HojaExcel(Excel.Worksheet hoja)
			:base(hoja)
		{
		}
	}
	public class RangoExcel:AccesoExcel{
		static object ___ = Type.Missing; 		
		internal RangoExcel(Excel.Range rango,Excel.Worksheet hoja)
			:base(rango,hoja)
		{
		}
		public int CantidadFilas{
			get{ return rango.Rows.Count;}
		}
		public int CantidadColumnas{
			get{ return rango.Columns.Count;}
		}
		public string[] TextoRango1D(){
			string[] rta=new string[CantidadFilas*CantidadColumnas];
			for(int i=1;i<=CantidadFilas;i++){
				for(int j=1;j<=CantidadColumnas;j++){
					rta[(i-1)*CantidadColumnas+j-1]=this.TextoCelda(i,j);
				}
			}
			return rta;
		}
		public object[] ValorRango1D(){
			object[] rta=new object[CantidadFilas*CantidadColumnas];
			for(int i=1;i<=CantidadFilas;i++){
				for(int j=1;j<=CantidadColumnas;j++){
					rta[(i-1)*CantidadColumnas+j-1]=this.ValorCelda(i,j);
				}
			}
			return rta;
		}
	}
	public class AccesoExcel{
		internal Excel.Worksheet hoja;
		internal Excel.Range rango;
		static object ___ = Type.Missing; 		
		protected AccesoExcel(Excel.Worksheet hoja):
			this(hoja.get_Range("A1",___),hoja)
		{
		}
		protected AccesoExcel(Excel.Range rango,Excel.Worksheet hoja){
			this.rango=rango;
			this.hoja=hoja;
		}
		public string TextoCelda(string rango){
			return this.rango.get_Range(rango,___).Text.ToString();
		}
		public string TextoCelda(int fila, int col){
			return ((Excel.Range) rango.Cells[fila,col]).Text.ToString();
		}
		static object ValorCelda(Excel.Range rango){
			object valor=rango.Value2;
			if(valor!=null && valor.GetType()==typeof(double)){
				string loQueSeVe=rango.Text.ToString();
				if(loQueSeVe.IndexOf('/')>0 || loQueSeVe.IndexOf('-')>0){
					return new DateTime(1900,1,1).AddDays((double)valor-2);
				}
			}
			return valor;
		}
		public object ValorCelda(string rango){
			return ValorCelda(this.rango.get_Range(rango,___));
		}
		public object ValorCelda(int fila, int col){
			return ValorCelda((Excel.Range) rango.Cells[fila,col]);
		}
		public void PonerTexto(int fila,int col,string valor){
			((Excel.Range) rango.Cells[fila,col]).Value2=valor;
		}
		static void PonerValor(Excel.Range rango,object valor){
			rango.Value2=valor;
			if(valor!=null && valor.GetType()==typeof(DateTime)){
				if(LibroExcel.FormatoFechas==null){
					rango.NumberFormat="dd/mm/yyyy";
					if(rango.Text.ToString().IndexOf("y")>0){
						LibroExcel.FormatoFechas="dd/mm/aaaa";
					}else{
						LibroExcel.FormatoFechas="dd/mm/yyyy";
					}
				}
				rango.NumberFormat=LibroExcel.FormatoFechas;
			}
		}
		public void PonerValor(int fila,int col,object valor){
			PonerValor((Excel.Range) rango.Cells[fila,col],valor);
		}
		public void Rellenar(string[,] matriz){
			for(int fila=0;fila<matriz.GetLength(0);fila++){
				for(int col=0;col<matriz.GetLength(1);col++){
					PonerTexto(fila+1,col+1,matriz[fila,col]);
				}
			}
		}
		public void Rellenar(object[,] matriz){
			for(int fila=0;fila<matriz.GetLength(0);fila++){
				for(int col=0;col<matriz.GetLength(1);col++){
					PonerValor(fila+1,col+1,matriz[fila,col]);
				}
			}
		}
		public RangoExcel Rango(string esquina,string otraEsquina){
			return new RangoExcel(rango.get_Range(esquina,otraEsquina),hoja);
		}
		public RangoExcel Rango(string definicionRango){
			return new RangoExcel(rango.get_Range(definicionRango,___),hoja);
		}
		public RangoExcel Columna(int columna){
			return new RangoExcel(
				rango.get_Range(
					hoja.Cells[1,columna],
					hoja.Cells[rango.Rows.Count,columna]
				),hoja
			);
		}
		public RangoExcel Fila(int fila){
			return new RangoExcel(
				rango.get_Range(
					hoja.Cells[fila,1],
					hoja.Cells[fila,rango.Columns.Count]
				),hoja
			);
		}
	}
	public class ColeccionExcel{
		System.Collections.Generic.Dictionary<string, LibroExcel> libros;
		public ColeccionExcel(){
			libros=new System.Collections.Generic.Dictionary<string, LibroExcel>();
		}
		public void Abrir(string nombreArchivo){
			if(libros.ContainsKey(nombreArchivo)){
				LibroExcel libro=libros[nombreArchivo];
				if(!libro.Abierto){
					libro=LibroExcel.Abrir(nombreArchivo);
					libros[nombreArchivo]=libro;
				}
			}else{
				LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
				libros.Add(nombreArchivo,libro);
			}
		}
		public LibroExcel this[string nombreArchivo]{
			get{
				return libros[nombreArchivo];
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
			libro.CerrarNoHayCambios();
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
			Assert.AreEqual("1",hoja.ValorCelda("B1"));
			Assert.AreEqual(1,hoja.ValorCelda("C1"));
			Assert.AreNotEqual(hoja.ValorCelda("B1").GetType(),hoja.ValorCelda("C1").GetType());
			Assert.AreEqual(0,hoja.ValorCelda("E1"));
			Assert.AreEqual(null,hoja.ValorCelda("F1"));
			Assert.AreEqual(null,hoja.ValorCelda("G1"));
			Assert.AreEqual(" ",hoja.ValorCelda("H1"));
			libro.CerrarNoHayCambios();
		}
		[Test]
		public void UltimoFilasyColumnas(){
			LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
			RangoExcel rango=libro.Rango("B2","N3");
			RangoExcel columnafinal=rango.Columna(13);
			Assert.AreEqual(1,columnafinal.CantidadColumnas);
			Assert.AreEqual(2,columnafinal.CantidadFilas);
			Assert.AreEqual("pi",columnafinal.ValorCelda("A2"));
			Assert.AreEqual("pi",columnafinal.ValorCelda(2,1));
			RangoExcel r2=rango.Rango("A2", "B3");
			rango.PonerValor(2,1,21);
			rango.PonerValor(2,2,22);
			rango.PonerValor(3,1,"po");
			rango.PonerValor(3,2,"pu");
			string[] contenido=r2.TextoRango1D();
			Assert.AreEqual(new string[]{"21","22","po","pu"},contenido);
			libro.GuardarYCerrar();
		}
		[Test]
		public void TipoFecha(){
			string nombreArchivo=Archivo.CarpetaActual()+"\\borrar_prueba_fechas.xls";
			LibroExcel libro=LibroExcel.Nuevo();
			libro.PonerValor(2,2,new DateTime(2008,3,2));
			Assert.AreEqual(new DateTime(2008,3,2),libro.ValorCelda(2,2));
			Assert.AreEqual("02/03/2008",libro.TextoCelda("B2"));
			//Archivo.Borrar(nombreArchivo);
			//libro.GuardarYCerrar(nombreArchivo);
			libro.DescartarYCerrar();
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
			// hoja1.Activate();
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
			hoja.Cells[1,2]="'1";
			hoja.Cells[1,3]=1;
			hoja.Cells[1,4]=0;
			hoja.Cells[1,5]="0";
			// hoja.Cells[1,5]=0; queda en blanco
			hoja.Cells[1,7]=""; // string sin caracteres
			hoja.Cells[1,8]=" "; // string con espacio
			hoja.Cells[2,2]="dos";
			hoja.Cells[3,14]="pi";
			hoja.Cells[4,15]="algo";
			hoja.Cells[2,13]="nada";
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
			Assert.AreEqual("1",hoja.get_Range("B1",___).Text);
			Assert.AreEqual("1",hoja.get_Range("B1",___).Value2);
			Assert.AreEqual("1",hoja.get_Range("C1",___).Text);
			Assert.AreEqual(1,hoja.get_Range("C1",___).Value2);
			Assert.AreNotEqual(hoja.get_Range("B1",___).Value2.GetType(),hoja.get_Range("C1",___).Value2.GetType());
			Assert.AreEqual(0,hoja.get_Range("E1",___).Value2);
			Assert.AreEqual(null,hoja.get_Range("F1",___).Value2);
			Assert.AreEqual(null,hoja.get_Range("G1",___).Value2);
			Assert.AreEqual(" ",hoja.get_Range("H1",___).Value2);
			libro.Close(___,___,___);
		}
		[Test]
		public void UltimoFilasYColumnas(){
			Excel.Workbook libro=ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___);
			Excel.Worksheet hoja=(Excel.Worksheet) libro.Worksheets["LaHoja1"];
			// Excel.Range r=hoja.Range["A1"];
			Excel.Range r;
			r=hoja.get_Range("B2","N3");
			Assert.AreEqual(2,r.Rows.Count);
			Assert.AreEqual(13,r.Columns.Count);
			Assert.AreEqual("pi",((Excel.Range) r.Cells[2,13]).Value2);
			Assert.AreEqual("pi",r.get_Range("M2",___).Value2);
			Assert.AreEqual("dos",r.get_Range("A1",___).Text);
			//Assert.AreEqual("dos",r.Cells[1,1]);
			// Excel.Range r2=r.get_Range("F13C1","F13C2");
			// Excel.Range r2=r.get_Range("M1","M2");
			// Excel.Range r2=r.get_Range(r.Cells[1,13],r.Cells[2,13]);
			Excel.Range r2=r.get_Range(hoja.Cells[1,13],hoja.Cells[2,13]);
			Assert.AreEqual(2,r2.Rows.Count);
			Assert.AreEqual(1,r2.Columns.Count);
			Assert.AreEqual("pi",r2.get_Range("A2",___).Text);
			Excel.Range r3=r.get_Range("M1","M2");
			Assert.AreEqual(2,r3.Rows.Count);
			Assert.AreEqual(1,r3.Columns.Count);
			Assert.AreEqual("pi",r3.get_Range("A2",___).Text);
			libro.Close(___,___,___);
		}
		[Test]
		public void TipoFecha(){
			/* Todo esto no camina, ni el close ni las fechas
			Excel.Workbook libro=ApExcel.Workbooks.Open(nombreArchivo,___,___,___,___,___,___,___,___,___,___,___,___,___,___);
			Excel.Worksheet hoja=(Excel.Worksheet) libro.Worksheets["LaHoja1"];
			hoja.Cells[4,4]="2/3/2008";
			Assert.AreEqual(new DateTime(2008,3,2),hoja.get_Range("D4",___).Value2);
			
			ApExcel.ActiveWindow;
			libro.Close(false,___,___);
			*/
		}
	}
}
#endif
