/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 15/04/2008
 * Hora: 11:34
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificaci�n | Editar Encabezados Est�ndar
 */

using System;
using System.Data;
using System.Data.Common;
using System.Windows.Forms;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using ModeladorSql;
using Interactivo;
	
namespace Tareas
{
	public class TablaTmp:Tabla{
		public TablaTmp()
			:base()
		{
			NombreTabla="tmp_cp_"+NombreTabla;
		}
	}
	public class ComparacionPadrones:Repositorio{
		/*
		[Vista]
		class EntradaCallesViejas{
			
		}
		class EntradaCallesNuevas{
			
		}
		*/
		public class CampoConjunto:CampoChar{ public CampoConjunto():base(20){} };
		public class CampoLote:CampoChar{ public CampoLote():base(20){} };
		public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
		public class CampoMetodo:CampoChar{ public CampoMetodo():base(20){} };
		public class CampoPalabra:CampoChar{ public CampoPalabra():base(20){} };
		public class CampoDescripcion:CampoChar{ public CampoDescripcion():base(250){} };
		public class Nombres:TablaTmp{
			[Pk] public CampoConjunto cConjunto;
			[Pk] public CampoNombre cNombre;
			public CampoNombre cNombreReordenado;
			public CampoLogico cUsado;
		}
		public class Resultados:TablaTmp{
			[Pk] public CampoMetodo cMetodo;
			public CampoDescripcion cDescripcion;
		}
		public class Asignacion:TablaTmp{
			[Pk] public CampoNombre cNombreViejo;
			public CampoNombre cNombreNuevo;
			public CampoMetodo cMetodo;
			[Fk] public Resultados fkResultados;
		}
		public class NombresPalabras:TablaTmp{
			[Pk] public CampoNombre cNombre;
			[Pk] public CampoEntero cOrden;
			public CampoPalabra cPalabra;
		}
		public class Diccionario:TablaTmp{
			[Pk] public CampoLote cLote;
			[Pk] public CampoPalabra cPalabra;
			[Pk] public CampoPalabra cPalabraNormalizada;
		}
		public ComparacionPadrones(BaseDatos db)
			:base(db)
		{
		}
		public void AsignacionOrdenada(string queMetodo){
			Resultados res=new Resultados();
			Asignacion a=new Asignacion();
			Nombres n=new Nombres();
			n.Alias="n";
			Nombres v=new Nombres();
			v.Alias="v";
			using(Ejecutador ej=new Ejecutador(db)){
				res.InsertarValores(db,res.cMetodo.Es(queMetodo),res.cDescripcion.Es(queMetodo));
				ej.Ejecutar(
					new SentenciaInsert(a)
						.Select(a.cNombreViejo.Es(v.cNombre),a.cNombreNuevo.Es(n.cNombre),a.cMetodo.Es(queMetodo))
						.Where(v.cNombreReordenado.Igual(n.cNombreReordenado)
						       ,v.cConjunto.Igual("viejo")
						       ,n.cConjunto.Igual("nuevo")
						       ,v.cUsado.Igual(false)
						       ,n.cUsado.Igual(false))
				);
				ej.Ejecutar(
					new SentenciaUpdate(n,n.cUsado.Es(true))
					.Where(n.cConjunto.Igual("nuevo")
					       ,n.cNombreReordenado.Igual(a.cNombreNuevo))
				);
				ej.Ejecutar(
					new SentenciaUpdate(n,n.cUsado.Es(true))
					.Where(n.cConjunto.Igual("viejo")
					       ,n.cNombreReordenado.Igual(a.cNombreViejo))
				);
			}
		}
		public void AsignacionExacta(){
			Asignacion a=new Asignacion();
			Nombres n=new Nombres();
			n.Alias="n";
			using(Ejecutador ej=new Ejecutador(db)){
				ej.Ejecutar(
					new SentenciaUpdate(n,n.cNombreReordenado.Es(n.cNombre))
				);
			}
			AsignacionOrdenada("exacta");
		}
		public void NormalizarPalabras(){
			NombresPalabras np=new NombresPalabras();
			using(Ejecutador ej=new Ejecutador(db)){
				ej.Ejecutar(
					new SentenciaUpdate(np,np.cPalabra.Es(Fun.Normalizar(np.cPalabra)))
				);
			}
		}
		public void PasarDiccionario(string lote){
			NombresPalabras np=new NombresPalabras();
			Diccionario d=new Diccionario();
			using(Ejecutador ej=new Ejecutador(db)){
				ej.Ejecutar(
					new SentenciaUpdate(np,np.cPalabra.Es(d.cPalabraNormalizada))
					.Where(np.cPalabra.Igual(d.cPalabra),d.cLote.Igual(lote))
				);
			}
		}
		public void SepararPalabras(){
			Nombres nombres=new ComparacionPadrones.Nombres();
			NombresPalabras nomprespalabras=new NombresPalabras();
			ListaCampos pkEnAmbos=nombres.CamposPk();
			pkEnAmbos.RemoveAll(campo => !nomprespalabras.ContieneMismoNombre(campo));
			db.ExecuteNonQuery(@"CREATE OR REPLACE FUNCTION SEPARARPALABRAS() RETURNS VOID AS
$$
declare
	pos integer;
	v_palabra character varying (200);
	v_frase character varying (200);
	c record;
	i integer;
begin
	for c in select {PKS} from {TABLA} GROUP BY {PKS} loop
		v_frase:=c.nombre;
		v_palabra:='';
		i:=1;
		loop
			v_palabra=primerpalabra(v_frase);
			v_frase=sinprimerpalabra(v_frase);
			begin
				insert into {TABLA}palabras ({PKS},palabra,orden) values ({C.PKS},v_palabra,i);
				i:=i+1;
			exception 
				when unique_violation then
				    null;
			end;
		exit when v_frase is null or v_frase='';
		end loop;
	end loop;
end;
$$
LANGUAGE plpgsql".Replace("{TABLA}",nombres.NombreTabla)
	               .Replace("{PKS}",Separador.Concatenar(pkEnAmbos.ConvertAll(campo => campo.NombreCampo),","))
	               .Replace("{C.PKS}",Separador.Concatenar(pkEnAmbos.ConvertAll(campo => "c."+campo.NombreCampo),","))
	);
			db.ExecuteNonQuery("SELECT SEPARARPALABRAS();");
		}
		public void ReordenarPalabras(){
			Nombres nombres=new ComparacionPadrones.Nombres();
			NombresPalabras nomprespalabras=new NombresPalabras();
			ListaCampos pkEnAmbos=nombres.CamposPk();
			pkEnAmbos.RemoveAll(campo => !nomprespalabras.ContieneMismoNombre(campo));
			db.ExecuteNonQuery(@"CREATE OR REPLACE FUNCTION REORDENARPALABRAS() RETURNS VOID AS
$$
begin
	declare
		c_frase record;
		c_palabra record;
		ordenada text;
	begin
		update {TABLA} set nombrereordenado=null where usado='N';
		for c_frase in select * from {TABLA} where nombrereordenado is null loop
			ordenada:='';
			for c_palabra in 
				select * 
					from {TABLA}palabras
					where {JOIN}
					order by palabra 
			loop
				ordenada:=ordenada||c_palabra.palabra||' ';
			end loop;
			begin
				update {TABLA}
					set nombreReordenado=ordenada
					where {JOIN};
			end;
		end loop;
	end;
end;
$$
LANGUAGE plpgsql".Replace("{TABLA}",nombres.NombreTabla)
			                   .Replace("{JOIN}",Separador.Concatenar(pkEnAmbos.ConvertAll(campo => campo.NombreCampo+"=c_frase."+campo.NombreCampo)," AND ")));
			db.ExecuteNonQuery("SELECT REORDENARPALABRAS();");
		}
		public void PrepararCorrespondencia(){
			AsignacionExacta();
			SepararPalabras();
			ReordenarPalabras();
			AsignacionOrdenada("reordenadas");
			NormalizarPalabras();
			ReordenarPalabras();
			AsignacionOrdenada("normalizadas");
			PasarDiccionario("ABR");
			ReordenarPalabras();
			AsignacionOrdenada("abreviaturas");
		}
		public void Mostrar(){
			var asignacion=new Asignacion();
			asignacion.UsarFk();
			Application.Run(new GrillaBaseDatos(db,asignacion.fkResultados,asignacion));
		}
		public void InsertarDiccionarioPropuesto(){
			new Ejecutador(db).EjecutrarSecuencia(
@"insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','PRIMERO','1');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','UNO','1');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DOS','2');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','TRES','3');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','CUATRO','4');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','CINCO','5');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','SEIS','6');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','SIETE','7');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','OCHO','8');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','NUEVE','9');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DIEZ','10');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','ONCE','11');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DOCE','12');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','TRECE','13');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','CATORCE','14');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','QUINCE','15');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DIECISEIS','16');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DIECISIETE','17');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DIECIOCHO','18');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','DIECINUEVE','19');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTE','20');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTIUNO','21');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTIDOS','22');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTITRES','23');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINICUATRO','24');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTICINCO','25');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTISEIS','26');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTISIETE','27');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTIOCHO','28');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','VEINTINUEVE','29');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','TREINTA','30');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','TREINTIUNO','31');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','TREINTIDOS','32');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('NUMERO','TREINTITRES','33');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('HOMONIMOS','SETIEMBRE','SEPTIEMBRE');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('ABREV','PASAJE','PJE');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('ABREV','AVENIDA','AV');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('ABREV','PRESIDENTE','PRES');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('ABREV','DOCTOR','DR');
insert into tmp_cp_diccionario (lote,palabra,palabraNormalizada) values ('ABREV','CORONOEL','CNEL');"
			);
		}
	}
	[TestFixture]
	public class PrComparacionPadrones{
		BaseDatos db;
		ComparacionPadrones cp;
		public PrComparacionPadrones(){
			#pragma warning disable 162
			switch(1){ // Solo se va a tomar un camino
				case 1:
					db=PostgreSql.Abrir("127.0.0.1","import2sqldb","import2sql","sqlimport");
					cp=new ComparacionPadrones(db);
					cp.EliminarTablas();
					cp.CrearTablas();
					break;
				case 3:
					string nombreBase="comparaciones.mdb";
					Archivo.Borrar(nombreBase);
					BdAccess.Crear(nombreBase);
					db=BdAccess.Abrir(nombreBase);
					cp=new ComparacionPadrones(db);
					cp.CrearTablas();
					break;
			}
			#pragma warning restore 162
			ComparacionPadrones.Diccionario diccionario=new Tareas.ComparacionPadrones.Diccionario();
			diccionario.InsertarDirecto(db,"ABR","San","Sn");
			diccionario.InsertarDirecto(db,"ABR","Doctor","Dr");
			diccionario.InsertarDirecto(db,"ABR","Doctora","Dra");
		}
		public void Vaciar(){
			Ejecutador ej=new Ejecutador(db);
			ej.Ejecutar(new SentenciaDelete(new ComparacionPadrones.NombresPalabras()));
			ej.Ejecutar(new SentenciaDelete(new ComparacionPadrones.Asignacion()));
			ej.Ejecutar(new SentenciaDelete(new ComparacionPadrones.Nombres()));
			ej.Ejecutar(new SentenciaDelete(new ComparacionPadrones.Resultados()));
		}
		public void Cargar1(string[] datos,string conjunto){
			ComparacionPadrones.Nombres n=new ComparacionPadrones.Nombres();
			foreach(string dato in datos){
				using(Insertador ins=new Insertador(db,n)){
					n.cConjunto[ins]=conjunto;
					n.cNombre[ins]=dato;
				}
			}
		}
		public void Cargar(string[] viejos,string[] nuevos){
			Cargar1(viejos,"viejo");
			Cargar1(nuevos,"nuevo");
		}
		public void CompararCoincidencias(string[] viejosCoincidentes,string[] nuevosCoincidentes){
			int item=0;
			foreach(ComparacionPadrones.Asignacion a in new ComparacionPadrones.Asignacion().Todos(db)){
				Assert.AreEqual(viejosCoincidentes[item],a.cNombreViejo.Valor,"viejos coindicentes "+item);
				Assert.AreEqual(nuevosCoincidentes[item],a.cNombreNuevo.Valor,"nuevos coindicentes "+item);
				item++;
			}
			Assert.AreEqual(viejosCoincidentes.Length,item,"Debe coincidir la cantidad");
		}
		[Test]
		public void Pr01Exacta(){
			string[] viejos={"San Mart�n","Belgrano","Corrientes","No esta"," distinta "};
			string[] nuevos={"San Mart�n","Belgrano","Corrientes","Tampoco esta","distinta"};
			string[] coinciden={"Belgrano","Corrientes","San Mart�n"};
			Cargar(viejos,nuevos);
			cp.AsignacionExacta();
			CompararCoincidencias(coinciden,coinciden);
		}
		[Test]
		public void Pr02ExtraccionPalabras(){
			cp.SepararPalabras();
			var n=new ComparacionPadrones.Nombres();
			if(db is BdAccess){
				Assert.Ignore("Todav�a no se puede sacar la primera palabra en Access");
			}
			using(var ej=new Ejecutador(db)){
				var cPrimeraPalabra=new CampoDestino<string>("primerapalabra");
				var cSinPrimeraPalabra=new CampoDestino<string>("sinprimerapalabra");
				IDataReader dr=ej.EjecutarReader(
					new SentenciaSelect(n.cNombre
					                    ,cPrimeraPalabra.Es(n.cNombre.PrimeraPalabra())
					                    ,cSinPrimeraPalabra.Es(n.cNombre.SinPrimeraPalabra())
					                   )
					.Where(n.cConjunto.Igual("viejo"))
					.OrderBy(n.cNombre)
				);
				dr.Read();
				Assert.AreEqual(" distinta ",dr["nombre"]);
				Assert.AreEqual("distinta",dr["primerapalabra"]);
				// Assert.Ignore("Revisar con distintas configuraciones, a veces no anda lo que sigue");
				Assert.AreEqual("",dr["sinprimerapalabra"]);
				dr.Read();
				Assert.AreEqual("Belgrano",dr["primerapalabra"]);
				Assert.AreEqual("",dr["sinprimerapalabra"]);
				dr.Read();
				dr.Read();
				Assert.AreEqual("No",dr["primerapalabra"]);
				Assert.AreEqual("esta",dr["sinprimerapalabra"]);
			}
		}
		[Test]
		public void Pr03Reordenacion(){
			cp.ReordenarPalabras();
			string[] viejos={"San Mart�n","Belgrano","Corrientes","No esta"," distinta "};
			string[] nuevos={"Mart�n San","Belgrano","Corrientes","Tampoco esta","distinta"};
			string[] coinciden1={"Belgrano","Corrientes"};
			string[] coinciden2v={" distinta ","Belgrano","Corrientes","San Mart�n"};
			string[] coinciden2n={"distinta","Belgrano","Corrientes","Mart�n San"};
			Vaciar();
			Cargar(viejos,nuevos);
			cp.AsignacionExacta();
			CompararCoincidencias(coinciden1,coinciden1);
			cp.SepararPalabras();
			cp.ReordenarPalabras();
			cp.AsignacionOrdenada("reordenada");
			CompararCoincidencias(coinciden2v,coinciden2n);			
		}
		[Test]
		public void Pr04Normalizadas(){
			cp.ReordenarPalabras();
			string[] viejos={"San Mart�n","Belgrano","Corrientes","No esta"};
			string[] nuevos={"Martin San","Belgrano","Corrientes","Tampoco esta"};
			string[] coinciden1={"Belgrano","Corrientes"};
			string[] coinciden2v={"Belgrano","Corrientes","San Mart�n"};
			string[] coinciden2n={"Belgrano","Corrientes","Martin San"};
			Vaciar();
			Cargar(viejos,nuevos);
			cp.AsignacionExacta();
			cp.SepararPalabras();
			cp.ReordenarPalabras();
			cp.AsignacionOrdenada("reordenada");
			CompararCoincidencias(coinciden1,coinciden1);
			cp.NormalizarPalabras();
			cp.ReordenarPalabras();
			cp.AsignacionOrdenada("Normalizadas");
			CompararCoincidencias(coinciden2v,coinciden2n);			
		}
		[Test]
		public void Pr05ViaDiccionario(){
			cp.ReordenarPalabras();
			cp.ReordenarPalabras();
			string[] viejos={"Sn Mart�n","Belgrano","Corrientes","No esta","Dr. R�mulo Na�n"};
			string[] nuevos={"Martin San","Belgrano","Corrientes","Tampoco esta","Doctor Romulo Naon"};
			string[] coinciden1={"Belgrano","Corrientes"};
			string[] coinciden2v={"Belgrano","Corrientes","Dr. R�mulo Na�n","Sn Mart�n"};
			string[] coinciden2n={"Belgrano","Corrientes","Doctor Romulo Naon","Martin San"};
			Vaciar();
			Cargar(viejos,nuevos);
			cp.AsignacionExacta();
			cp.SepararPalabras();
			cp.ReordenarPalabras();
			cp.AsignacionOrdenada("reordenadas");
			cp.NormalizarPalabras();
			cp.ReordenarPalabras();
			cp.AsignacionOrdenada("normalizadas");
			CompararCoincidencias(coinciden1,coinciden1);
			cp.PasarDiccionario("ABR");
			cp.ReordenarPalabras();
			cp.AsignacionOrdenada("abreviaturas");
			CompararCoincidencias(coinciden2v,coinciden2n);	
			cp.Mostrar();
		}
	}
}
