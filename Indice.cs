/*
 * Creado por SharpDevelop.
 * Usuario: Andrea
 * Fecha: 21/03/2008
 * Hora: 19:42
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Data;
using NUnit.Framework;
using TodoASql;

namespace Indices
{
	/// <summary>
	/// Description of Indice.
	/// </summary>
	public class Repositorio
	{
		internal BaseDatos db;
		Repositorio(BaseDatos db){
			this.db=db;
		}
		public static Repositorio Crear(BaseDatos db){
			db.ExecuteNonQuery(@"
				create table productos(
					producto varchar(4) primary key,
					nombre varchar(250)
				);
			");
			db.ExecuteNonQuery(@"
				create table grupos(
					agrupacion varchar(9),
					grupo varchar(9),
					nombre varchar(250),
					grupopadre varchar(9),
					ponderador double precision,
					nivel integer,
					esproducto char(1) default 'N',
					primary key(agrupacion,grupo)
				);
			");
			db.ExecuteNonQuery(@"
				create table numeros(
					numero integer primary key
				);
			");
			for(int i=0; i<=20; i++){
				db.ExecuteNonQuery("insert into numeros (numero) values ("+i.ToString()+")");
			}
			return new Repositorio(db);
		}
		public static Repositorio Abrir(BaseDatos db){
			return new Repositorio(db);			
		}
		public Producto AbrirProducto(string codigo){
			return new Producto(this,codigo);
		}
		public Producto CrearProducto(string codigo){
			using(InsertadorSql ins=new InsertadorSql(db,"productos")){
				ins["producto"]=codigo;
				ins.InsertarSiHayCampos();
			}
			return AbrirProducto(codigo);
		}
		public Grupo AbrirGrupo(string codigo){
			return new Grupo(this,codigo);
		}
		public Grupo CrearGrupo(string codigo){
			return CrearGrupo(codigo,null,1);
		}
		public Grupo CrearGrupo(string codigo,Grupo padre,double ponderador){
			using(InsertadorSql ins=new InsertadorSql(db,"grupos")){
				ins["grupo"]=codigo;
				ins["ponderador"]=ponderador;
				if(padre==null){
					ins["agrupacion"]=codigo;
				}else{
					ins["agrupacion"]=padre.Agrupacion;
					ins["grupopadre"]=padre.Clave;
				}
				ins.InsertarSiHayCampos();
			}
			return AbrirGrupo(codigo);
		}
		public void CrearHoja(Producto producto,Grupo grupo,double ponderador){
			using(InsertadorSql ins=new InsertadorSql(db,"grupos")){
				ins["agrupacion"]=grupo.Agrupacion;
				ins["grupo"]=producto.Clave;
				ins["grupopadre"]=grupo.Clave;
				ins["ponderador"]=ponderador;
				ins["esproducto"]=db.Verdadero;
				ins.InsertarSiHayCampos();
			}			
		}
		public void CalcularPonderadores(Grupo grupo){
			SentenciaSql secuencia=new SentenciaSql(db,@"
				UPDATE grupos SET nivel=0
				  WHERE grupopadre is null 
				    AND agrupacion={agrupacion};
				UPDATE grupos h SET h.nivel=p.nivel+1
				  FROM grupos p
				  WHERE p.grupo=h.grupopadre
				    AND p.agrupacion=h.agrupacion 
				    AND h.agrupacion={agrupacion};
			").Arg("agrupacion",grupo.Agrupacion);
			db.EjecutrarSecuencia(secuencia);
		}
		public void ReglasDeIntegridad(){
			db.AssertSinRegistros(
				"La raiz de los grupos en una canasta debe tener el mismo código de la agrupación que define",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is null and grupo<>agrupacion
			");
			db.AssertSinRegistros(
				"Solo la raiz de los grupos en una canasta debe tener el mismo código de la agrupación que define",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is not null and grupo=agrupacion
			");
			db.AssertSinRegistros(
				"Todos los grupos deben tener nivel",
			@"
				SELECT *
				  FROM grupos
				  WHERE nivel is null
			");
			db.AssertSinRegistros(
				"La raiz de los grupos debe tener nivel 0",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is null and nivel<>0
			");
			db.AssertSinRegistros(
				"Los niveles de los hijos deben ser exactamente 1 más que el padre",
			@"
				SELECT h.grupopadre, p.nivel as nivelpadre, h.grupo, h.nivel, p.nombre as nombrepadre, h.nombre
				  FROM grupos p inner join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.nivel<>h.nivel+1
			");
			db.AssertSinRegistros(
				"Los niveles de los hijos deben ser exactamente 1 más que el padre",
			@"
				SELECT h.grupopadre, p.nivel as nivelpadre, h.grupo, h.nivel, p.nombre as nombrepadre, h.nombre
				  FROM grupos p inner join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.nivel<>h.nivel+1
			");
			db.AssertSinRegistros(
				"Todas las agrupaciones que no son productos deben tener hijos",
			@"
				SELECT p.grupo,p.nombre
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.esproducto='N'
				    AND h.grupo is null
			");
			db.AssertSinRegistros(
				"Todas las agrupaciones que son productos no deben tener hijos",
			@"
				SELECT p.grupo,p.nombre,h.grupo as grupohijo,h.nombre as nombrehijo
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.esproducto='S'
				    AND h.grupo is not null
			");
			db.AssertSinRegistros(
				"La suma de los ponderadores debe ser igual al poderador del padre",
			@"
				SELECT p.grupo,p.nombre,p.ponderador,sum(h.ponderador)
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  HAVING abs(p.ponderador)-sum(h.ponderador)>0.00000000000001
			");
			db.AssertSinRegistros(
				"Solo deben ser hojas los productos",
			@"
				SELECT g.grupo, g.nombre, p.producto, p.nombre as nombreproducto
				  FROM grupos g inner join productos p ON g.grupo=p.producto
				  WHERE g.esproducto='N'
			");
			db.AssertSinRegistros(
				"Si esta marcado como producto debe existir el producto",
			@"
				SELECT g.grupo, g.nombre, p.producto, p.nombre as nombreproducto
				  FROM grupos g left join productos p ON g.grupo=p.producto
				  WHERE g.esproducto='S' AND p.producto is null
			");
			db.AssertSinRegistros(
				"Los ponderadores de cada nivel deben sumar 1",
			@"
				SELECT n.numero as nivel, suma(ponderadores)
				  FROM numeros as n, grupos as g
				  WHERE g.nivel=n.numero OR g.esproducto='S' AND g.nivel<n.numero
			");
		}
	}
	public class Tabla:IDisposable{
		protected Repositorio Repo;
		string NombreTabla;
		public string Clave;
		string[] Claves;
		protected IDataReader Registro;
		protected Tabla(Repositorio repo,string nombreTabla,string clave){
			this.Repo=repo;	
			this.NombreTabla=nombreTabla;
			this.Clave=clave;
			this.Claves=new string[1];
			this.Claves[0]=clave;
			this.Registro=repo.db.ExecuteReader("SELECT * FROM "+repo.db.StuffTabla(nombreTabla));
			this.Registro.Read();
		}
		public void Dispose(){
			Registro.Close();
		}
	}
	public class Grupo:Tabla{
		public double Ponderador;
		public string Agrupacion;
		public Grupo(Repositorio repo,string codigo)
			:base(repo,"grupos",codigo)
		{
			Agrupacion=(string)Registro["grupo"];
			if(Registro["agrupacion"]!=null){
				Agrupacion=(string)Registro["agrupacion"];
			}
			if(Registro["ponderador"]!=null){
				Ponderador=(double)Registro["ponderador"];
			}
		}
	}
	public class Producto:Tabla{
		public Producto(Repositorio repo,string codigo)
			:base(repo,"productos",codigo)
		{}
		public double Ponderador(Grupo grupo){
			double wP=(double)
				Repo.db.ExecuteScalar(@"
					SELECT ponderador
					  FROM grupro 
					  WHERE agrupacion="+Repo.db.StuffValor(grupo.Agrupacion)+@"
					    AND producto="+Repo.db.StuffValor(Clave)
				);
			double wG=(double)
				Repo.db.ExecuteScalar(@"
					SELECT ponderador
					  FROM grupos 
					  WHERE agrupacion="+Repo.db.StuffValor(grupo.Agrupacion)+@"
					    AND grupo="+Repo.db.StuffValor(Clave)
				);
			return wP/wG;
		}
	}
	[TestFixture]
	public class ProbarIndiceD3{
		Repositorio repo;
		public ProbarIndiceD3(){
			string archivoMDB="indices_canastaD3.mdb";
			Archivo.Borrar(archivoMDB);
			BdAccess.Crear(archivoMDB);
			BdAccess db=BdAccess.Abrir(archivoMDB);
			repo=Repositorio.Crear(db);
			Producto P100=repo.CrearProducto("P100");
			Producto P101=repo.CrearProducto("P101");
			Producto P102=repo.CrearProducto("P102");
			Grupo A=repo.CrearGrupo("A");
			Grupo A1=repo.CrearGrupo("A1",A,60);
			Grupo A2=repo.CrearGrupo("A2",A,40);
			repo.CrearHoja(P100,A1,60);
			repo.CrearHoja(P101,A1,40);
			repo.CrearHoja(P102,A2,100);
			repo.CalcularPonderadores(A);
			/*
			Periodo pAnt=repo.CrearPeriodo(2001,12);
			repo.RegistrarPromedios(pAnt,P100,2.0);
			repo.RegistrarPromedios(pAnt,P101,10.0);
			repo.CalcularMesBase();
			*/
		}
		[Test]
		public void VerCanasta(){
			Grupo A=repo.AbrirGrupo("A");
			Grupo A1=repo.AbrirGrupo("A1");
			Producto P100=repo.AbrirProducto("P100");
			Assert.AreEqual(1.0,A.Ponderador);
			Assert.AreEqual(0.6,A1.Ponderador);
			Assert.AreEqual(0.36,P100.Ponderador(A));
			Assert.AreEqual(0.6,P100.Ponderador(A1));
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
	}
}
