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
			db.ExecuteNonQuery(@"
				CREATE TABLE periodos(
				    ano integer,
				    mes integer,
				    anoant integer,
				    mesant integer,
				    primary key(ano,mes),
				    foreign key (anoant,mesant) references periodos (ano,mes)
				);
			");
			db.ExecuteNonQuery(@"
				CREATE TABLE calpro(
				    ano integer,
				    mes integer,
				    producto varchar(9),
				    promedio double precision,
				    primary key(ano,mes,producto),
				    foreign key (ano,mes) references periodos (ano,mes),
				    foreign key (producto) references productos (producto)
				);
			");
			db.ExecuteNonQuery(@"
				CREATE TABLE calgru(
				    ano integer,
				    mes integer,
				    agrupacion varchar(9),
				    grupo varchar(9),
				    indice double precision,
				    factor double precision,
				    primary key(ano,mes,agrupacion,grupo),
				    foreign key (ano,mes) references periodos (ano,mes),
				    foreign key (agrupacion,grupo) references grupos (agrupacion,grupo)
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
		public void RegistrarPromedio(Periodo per,Producto prod,double promedio){
			using(InsertadorSql ins=new InsertadorSql(db,"calpro")){
				ins["ano"]=per.Ano;
				ins["mes"]=per.Mes;
				ins["producto"]=prod.Clave;
				ins["promedio"]=promedio;
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
					UPDATE grupos SET nivel={nivel}+1
					  WHERE (grupopadre) 
						IN (SELECT grupo 
                             FROM grupos 
                             WHERE nivel={nivel} 
                               AND agrupacion={agrupacion})
                        AND agrupacion={agrupacion}
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
							UPDATE grupos SET ponderador=
							    (SELECT sum(h.ponderador)
							       FROM grupos h
							       WHERE h.grupopadre=grupos.grupo
							         AND h.agrupacion=grupos.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}
				}
				for(int i=1;i<10;i++){
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						INSERT INTO auxgrupos (agrupacion,grupo,ponderadororiginal,sumaponderadorhijos)
						  SELECT p.agrupacion,p.grupo,p.ponderador,sum(h.ponderador)
						    FROM grupos p INNER JOIN grupos h ON p.agrupacion=h.agrupacion AND p.grupo=h.grupopadre
						    WHERE h.agrupacion={agrupacion} 
	                          AND h.nivel={nivel}
						    GROUP BY p.agrupacion,p.grupo,p.ponderador;
					").Arg("nivel",i));
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos h INNER JOIN auxgrupos a ON a.grupo=h.grupopadre AND a.agrupacion=h.agrupacion
                              SET h.ponderador=h.ponderador*a.ponderadororiginal/a.sumaponderadorhijos
							  WHERE h.agrupacion={agrupacion} 
		                        AND h.nivel={nivel}
						").Arg("nivel",i));
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos SET ponderador=ponderador
							    *(SELECT a.ponderadororiginal/a.sumaponderadorhijos
							       FROM auxgrupos a 
							       WHERE a.grupo=grupos.grupopadre
							         AND a.agrupacion=grupos.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
						").Arg("nivel",i));
					}
				}
			}
		}
		public void CalcularMesBase(Periodo per,Agrupacion agrupacion){
			using(EjecutadorSql ej=db.Ejecutador("ano",per.Ano,"mes",per.Mes,"agrupacion",agrupacion.Agrupacion)){
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select {ano},{mes},agrupacion,grupo,100,1
					    from grupos
					    where agrupacion={agrupacion};
				");
			}
		}
		public void CalcularCalGru(Periodo periodo,Agrupacion agrupacion){
			using(EjecutadorSql ej=db.Ejecutador("ano",periodo.Ano,"mes",periodo.Mes,"agrupacion",agrupacion.Agrupacion)){
				/*
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select per.ano,per.mes,g.agrupacion,g.grupo,cg0.indice*cp1.promedio/cp0.promedio,cg0.factor
					    from ((((grupos as g 
							 inner join productos as p on g.grupo=p.producto)
					         inner join calgru as cg0 on g.grupo=cg0.grupo and g.agrupacion=cg0.agrupacion)
					         inner join periodos as per on per.anoant=cg0.ano and per.mesant=cg0.mes)
					         inner join calpro as cp0 on cp0.ano=per.anoant and cp0.mes=per.mesant and cp0.producto=p.producto)
					         inner join calpro as cp1 on cp1.ano=per.ano and cp1.mes=per.mes and cp1.producto=p.producto
					    where g.agrupacion={agrupacion}
					      and per.ano={ano}
					      and per.mes={mes}
				");
				*/
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select per.ano,per.mes,g.agrupacion,g.grupo,cg0.indice*cp1.promedio/cp0.promedio,cg0.factor
					    from grupos as g, 
							 productos as p,
					         calgru as cg0,
					         periodos as per, 
					         calpro as cp0, 
					         calpro as cp1
					    where g.agrupacion={agrupacion}
					      and per.ano={ano}
					      and per.mes={mes}
					      and g.grupo=p.producto
					      and g.grupo=cg0.grupo and g.agrupacion=cg0.agrupacion
					      and per.anoant=cg0.ano and per.mesant=cg0.mes
					      and cp0.ano=per.anoant and cp0.mes=per.mesant and cp0.producto=p.producto
					      and cp1.ano=per.ano and cp1.mes=per.mes and cp1.producto=p.producto
				");
				for(int i=9;i>=0;i--){
					/*
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
						  select cg.ano,cg.mes,gp.agrupacion,gp.grupo
								,sum(cg.indice*gh.ponderador)/sum(gh.ponderador)
								,sum(cg.factor*gh.ponderador)/sum(gh.ponderador)
						    from (grupos gh
						         inner join calgru as cg on gh.grupo=cg.grupo and gh.agrupacion=cg.agrupacion)
						         inner join grupos gp on gp.agrupacion=gh.agrupacion and gp.grupo=gh.grupopadre
						    where cg.agrupacion={agrupacion}
						      and cg.ano={ano}
						      and cg.mes={mes}
						      and gh.nivel={nivel}
						    group by cg.ano,cg.mes,gp.agrupacion,gp.grupo;
					").Arg("nivel",i));
					*/
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
						  select cg.ano,cg.mes,gp.agrupacion,gp.grupo
								,sum(cg.indice*gh.ponderador)/sum(gh.ponderador)
								,sum(cg.factor*gh.ponderador)/sum(gh.ponderador)
						    from grupos gh,
						         calgru as cg,
						         grupos gp 
						    where cg.agrupacion={agrupacion}
						      and cg.ano={ano}
						      and cg.mes={mes}
						      and gh.nivel={nivel}
						      and gh.grupo=cg.grupo and gh.agrupacion=cg.agrupacion
						      and gp.agrupacion=gh.agrupacion and gp.grupo=gh.grupopadre
						    group by cg.ano,cg.mes,gp.agrupacion,gp.grupo;
					").Arg("nivel",i));
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
		protected IDataReader Registro;
		public System.Collections.Generic.List<Clave> Claves;
		StringBuilder Sentencia;
		protected Tabla(Repositorio repo,string nombreTabla,params object[] clavesPlanas){
			this.Repo=repo;	
			this.NombreTabla=nombreTabla;
			this.Claves=new System.Collections.Generic.List<Clave>();
			// this.Claves=clavesPlanas;
			Sentencia=new StringBuilder("SELECT * FROM "+repo.db.StuffTabla(nombreTabla));
			int i=0;
			while(i<clavesPlanas.Length){
				object parametro=clavesPlanas[i];
				if(parametro.GetType()==typeof(string)){
					AgregarClave((string)parametro,clavesPlanas[i+1]);
					i++; 
				}else if(parametro.GetType().IsSubclassOf(typeof(Tabla))){
					Tabla t=(Tabla)parametro;
					foreach(Clave c in t.Claves){
						AgregarClave(c.Campo,c.Valor);
					}
				}
				i++;
			}
			this.Registro=repo.db.ExecuteReader(Sentencia.ToString());
			this.Registro.Read();
		}
		public object this[string campo]{ 
			get{
				return Registro[campo];
			}
		}
		public void Dispose(){
			Registro.Close();
		}
		protected void AgregarClave(string Campo, object Valor){
			string and=(Claves.Count==0?" WHERE ":" AND ");
			Claves.Add(new Clave(Campo,Valor));
			Sentencia.Append(and+Repo.db.StuffCampo(Campo)+"="+Repo.db.StuffValor(Valor));
		}
		public struct Clave{
			public string Campo;
			public object Valor;
			public Clave(string campo, object valor){ this.Campo=campo; this.Valor=valor; } 
		}
	}
	public class Grupo:Tabla{
		public double Ponderador;
		public string Agrupacion;
		public new string Clave;
		public Grupo(Repositorio repo,string agrupacion,string grupo)
			:base(repo,"grupos","agrupacion",agrupacion,"grupo",grupo)
		{
			Clave=(string)Registro["grupo"];
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
	public class Agrupacion:Grupo{
		public Agrupacion(Repositorio repo,string agrupacion)
			:base(repo,agrupacion,agrupacion)
		{}
	}
	public class Producto:Tabla{
		public new string Clave;
		public Producto(Repositorio repo,string codigo)
			:base(repo,"productos","producto",codigo)
		{
			Clave=(string)Registro["producto"];
		}
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
	public class Periodo:Tabla{
		public int Ano;
		public int Mes;
		public Periodo(Repositorio repo,int ano,int mes)
			:base(repo,"periodos","ano",ano,"mes",mes)
		{
			Ano=(int)Registro["ano"];
			Mes=(int)Registro["mes"];
		}
		public static Periodo Crear(Repositorio repo,int ano,int mes,object anoant,object mesant){
			using(InsertadorSql ins=new InsertadorSql(repo.db,"periodos")){
				ins["ano"]=ano;
				ins["mes"]=mes;
				ins["anoant"]=anoant;
				ins["mesant"]=mesant;
				ins.InsertarSiHayCampos();
			}
			return new Periodo(repo,ano,mes);
		}
		public static Periodo Crear(Repositorio repo,int ano,int mes){
			return Crear(repo,ano,mes,null,null);
		}
		public static Periodo CrearProximo(Repositorio repo,Periodo anterior){
			return Crear(repo,(anterior.Mes==12?anterior.Ano+1:anterior.Ano)
			             ,(anterior.Mes==12?1:anterior.Mes+1),anterior.Ano,anterior.Mes);
		}
	}
	public class CalGru:Tabla{
		public double Indice;
		public CalGru(Repositorio repo,Periodo per,Grupo grupo)
			:base(repo,"calgru",per,grupo)
		{
			Indice=(double)Registro["indice"];
		}
	}
	[TestFixture]
	public class ProbarIndiceD3{
		Repositorio repo;
		public ProbarIndiceD3(){
			BaseDatos db;
			switch(1){
				case 1: // probar con postgre
					db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
					db.EliminarTablaSiExiste("calgru");
					db.EliminarTablaSiExiste("calpro");
					db.EliminarTablaSiExiste("periodos");
					db.EliminarTablaSiExiste("grupos");
					db.EliminarTablaSiExiste("productos");
					db.EliminarTablaSiExiste("numeros");
					db.EliminarTablaSiExiste("auxgrupos");
					break;
				case 2: // probar con sqlite
					string archivoSqLite="prueba_sqlite.db";
					Archivo.Borrar(archivoSqLite);
					db=SqLite.Abrir(archivoSqLite);
					break;
				case 3: // probar con access
					string archivoMDB="indices_canastaD3.mdb";
					Archivo.Borrar(archivoMDB);
					BdAccess.Crear(archivoMDB);
					db=BdAccess.Abrir(archivoMDB);
					break;
			}
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
		public void A01CalculosBase(){
			Periodo pAnt=Periodo.Crear(repo,2001,12);
			Producto P100=new Producto(repo,"P100");
			Producto P101=new Producto(repo,"P101");
			Producto P102=new Producto(repo,"P102");
			Agrupacion A=new Agrupacion(repo,"A");
			repo.RegistrarPromedio(pAnt,P100,2.0);
			repo.RegistrarPromedio(pAnt,P101,10.0);
			repo.RegistrarPromedio(pAnt,P102,20.0);
			repo.CalcularMesBase(pAnt,A);
			Assert.AreEqual(100.0,new CalGru(repo,pAnt,A).Indice);
			Periodo Per1=Periodo.CrearProximo(repo,pAnt);
			Assert.AreEqual(2001,Per1["anoant"]);
			Assert.AreEqual(2002,Per1.Ano);
			Assert.AreEqual(1,Per1.Mes);
			Grupo A1=repo.AbrirGrupo("A","A1");
			Grupo A2=repo.AbrirGrupo("A","A2");
			repo.RegistrarPromedio(Per1,P100,2.0);
			repo.RegistrarPromedio(Per1,P101,10.0);
			repo.RegistrarPromedio(Per1,P102,22.0);
			repo.CalcularCalGru(Per1,A);
			Assert.AreEqual(110.0,new CalGru(repo,Per1,A2).Indice);
			Assert.AreEqual(104.0,new CalGru(repo,Per1,A).Indice);
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
	}
}
