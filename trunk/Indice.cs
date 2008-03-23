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
using System.Text;
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
			db.ExecuteNonQuery(@"
				CREATE TABLE auxgrupos(
				    agrupacion varchar(9),
				    grupo varchar(9),
				    ponderadororiginal double precision,
				    sumaponderadorhijos double precision,
				    primary key(agrupacion,grupo)
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
		public Grupo AbrirGrupo(string agrupacion,string codigo){
			return new Grupo(this,agrupacion,codigo);
		}
		public Grupo CrearGrupo(string codigo){
			return CrearGrupo(codigo,null,1);
		}
		public Grupo CrearGrupo(string codigo,Grupo padre,double ponderador){
			string Agrupacion;
			using(InsertadorSql ins=new InsertadorSql(db,"grupos")){
				ins["grupo"]=codigo;
				ins["ponderador"]=ponderador;
				if(padre==null){
					Agrupacion=codigo;
				}else{
					Agrupacion=padre.Agrupacion;
					ins["grupopadre"]=padre.Clave;
				}
				ins["agrupacion"]=Agrupacion;
				ins.InsertarSiHayCampos();
			}
			return AbrirGrupo(Agrupacion,codigo);
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
			using(EjecutadorSql ej=db.Ejecutador("agrupacion",grupo.Agrupacion)){
				ej.ExecuteNonQuery(@"
					UPDATE grupos SET nivel=0,ponderador=1
					  WHERE grupopadre is null 
					    AND agrupacion={agrupacion};
				");
				for(int i=0;i<10;i++){
				ej.ExecuteNonQuery(new SentenciaSql(db,@"
					UPDATE grupos h SET h.nivel={nivel}+1
					  WHERE (h.grupopadre) 
						IN (SELECT grupo 
                             FROM grupos 
                             WHERE nivel={nivel} 
                               AND agrupacion={agrupacion})
                        AND h.agrupacion={agrupacion}
				").Arg("nivel",i));
				}
				for(int i=9;i>=0;i--){ // Subir ponderadores nulos
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos p SET p.ponderador=
							    DSum('ponderador','grupos','grupopadre=''' & grupo & ''' and agrupacion=''' & agrupacion & '''')
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos p SET p.ponderador=
							    (SELECT sum(h.ponderador)
							       FROM grupos h
							       WHERE h.grupopadre=p.grupo
							         AND h.agrupacion=p.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}
				}
				for(int i=1;i<10;i++){
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							INSERT INTO auxgrupos (agrupacion,grupo,ponderadororiginal,sumaponderadorhijos)
							  SELECT p.agrupacion,p.grupo,p.ponderador,sum(h.ponderador)
							    FROM grupos p INNER JOIN grupos h ON p.agrupacion=h.agrupacion AND p.grupo=h.grupopadre
							    WHERE h.agrupacion={agrupacion} 
		                          AND h.nivel={nivel}
							    GROUP BY p.agrupacion,p.grupo,p.ponderador;
						").Arg("nivel",i));
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos h INNER JOIN auxgrupos a ON a.grupo=h.grupopadre AND a.agrupacion=h.agrupacion
                              SET h.ponderador=h.ponderador*a.ponderadororiginal/a.sumaponderadorhijos
							  WHERE h.agrupacion={agrupacion} 
		                        AND h.nivel={nivel}
						").Arg("nivel",i));
						/*
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos h SET h.ponderador=h.ponderador*
							    (SELECT ponderadororiginal/sumaponderadorhijos
							       FROM auxgrupos a
							       WHERE a.grupo=h.grupopadre
							         AND a.agrupacion=h.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
						").Arg("nivel",i));
						*/
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos h SET h.ponderador=h.ponderador/
							    (SELECT sum(b.ponderador)
							       FROM grupos b 
							       WHERE b.grupopadre=h.grupopadre
							         AND b.agrupacion=h.agrupacion)
							    (SELECT p.ponderador
							       FROM grupos p
							       WHERE p.grupo=h.grupopadre
							         AND p.agrupacion=h.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
						").Arg("nivel",i));
					}
				}
			}
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
				SELECT h.grupopadre, p.nivel as nivelpadre, h.grupo, h.nivel, p.nombre as nombrepadre, h.nombre, p.nivel+1-h.nivel
				  FROM grupos p inner join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.nivel+1<>h.nivel
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
				  GROUP BY p.grupo,p.nombre,p.ponderador
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
				SELECT g.agrupacion,n.numero as nivel, sum(ponderador) as suma, sum(ponderador)-1 as diferencia
				  FROM numeros as n, grupos as g
				  WHERE g.nivel=n.numero OR g.esproducto='S' AND g.nivel<n.numero
				  GROUP BY g.agrupacion,n.numero
				  HAVING sum(ponderador)<>1;
			");
			db.AssertSinRegistros(
				"No debe haber dos códigos de grupo iguales en distintas agrupaciones",
			@"
				SELECT grupo, count(*) as cantidad, min(agrupacion) as primero, max(agrupacion) as ultimo
				  FROM grupos as g
				  WHERE esproducto='N'
				  GROUP BY grupo
				  HAVING count(*)>1
			");
			db.AssertSinRegistros(
				"No debe haber hijos sin padre",
			@"
				SELECT h.grupopadre,h.grupo,h.nombre
				  FROM grupos h left join grupos p on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.grupo IS NULL and h.grupopadre IS NOT NULL;
			");
		}
	}
	public class Tabla:IDisposable{
		protected Repositorio Repo;
		string NombreTabla;
		public string Clave;
		protected IDataReader Registro;
		protected Tabla(Repositorio repo,string nombreTabla,params string[] clavesPlanas){
			this.Repo=repo;	
			this.NombreTabla=nombreTabla;
			// this.Claves=clavesPlanas;
			StringBuilder sentencia=new StringBuilder("SELECT * FROM "+repo.db.StuffTabla(nombreTabla));
			Separador and=new Separador(" WHERE "," AND ");
			for(int i=0;i<clavesPlanas.Length/2;i++){
				sentencia.Append(and+repo.db.StuffCampo(clavesPlanas[i*2])+"="+repo.db.StuffValor(clavesPlanas[i*2+1]));
			}
			this.Registro=repo.db.ExecuteReader(sentencia.ToString());
			this.Registro.Read();
			Clave=(string)Registro[clavesPlanas[clavesPlanas.Length-2]];
		}
		public void Dispose(){
			Registro.Close();
		}
	}
	public class Grupo:Tabla{
		public double Ponderador;
		public string Agrupacion;
		public Grupo(Repositorio repo,string agrupacion,string grupo)
			:base(repo,"grupos","agrupacion",agrupacion,"grupo",grupo)
		{
			if(Registro["agrupacion"]!=null){
				Agrupacion=(string)Registro["agrupacion"];
			}
			if(Registro["ponderador"].GetType()!=typeof(DBNull)){
				object valor=Registro["ponderador"];
				System.Console.WriteLine("tipo = "+valor.GetType().Name);
				Ponderador=(double)Registro["ponderador"];
			}
		}
	}
	public class Producto:Tabla{
		public Producto(Repositorio repo,string codigo)
			:base(repo,"productos","producto",codigo)
		{}
		public double Ponderador(Grupo grupo){
			double wP=(double)
				Repo.db.ExecuteScalar(@"
					SELECT ponderador
					  FROM grupos 
					  WHERE agrupacion="+Repo.db.StuffValor(grupo.Agrupacion)+@"
					    AND grupo="+Repo.db.StuffValor(Clave)
				);
			double wG=(double)
				Repo.db.ExecuteScalar(@"
					SELECT ponderador
					  FROM grupos 
					  WHERE agrupacion="+Repo.db.StuffValor(grupo.Agrupacion)+@"
					    AND grupo="+Repo.db.StuffValor(grupo.Clave)
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
			Grupo T=repo.CrearGrupo("T");
			repo.CrearHoja(P100,A1,60);
			repo.CrearHoja(P101,A1,40);
			repo.CrearHoja(P102,A2,100);
			repo.CrearHoja(P100,T,60);
			repo.CrearHoja(P101,T,40);
			repo.CrearHoja(P102,T,100);
			repo.CalcularPonderadores(A);
			repo.CalcularPonderadores(T);
			/*
			Periodo pAnt=repo.CrearPeriodo(2001,12);
			repo.RegistrarPromedios(pAnt,P100,2.0);
			repo.RegistrarPromedios(pAnt,P101,10.0);
			repo.CalcularMesBase();
			*/
		}
		[Test]
		public void VerCanasta(){
			Grupo A=repo.AbrirGrupo("A","A");
			Grupo A1=repo.AbrirGrupo("A","A1");
			Producto P100=repo.AbrirProducto("P100");
			Assert.AreEqual(1.0,A.Ponderador);
			Assert.AreEqual(0.6,A1.Ponderador,0.00000001);
			Assert.AreEqual(0.36,P100.Ponderador(A));
			Assert.AreEqual(0.6,P100.Ponderador(A1));
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
	}
}
