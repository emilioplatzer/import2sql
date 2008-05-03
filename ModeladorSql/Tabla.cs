/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 11:16 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Data;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public delegate bool Filtro(Campo c);
	public class Tabla:IConCampos{
		Lista<Campo> campos;
		public string Alias;
		public string NombreTabla;
		public BaseDatos db;
		public int CantidadCamposPk;
		public bool IniciadasFk=false;
		public Tabla TablaRelacionada;
		public Lista<Tabla> TablasFk;
		public System.Collections.Generic.Dictionary<string, Campo> CamposFkAlias=new System.Collections.Generic.Dictionary<string, Campo>();
		public Fk.Tipo TipoFk=Fk.Tipo.Obligatoria;
		public bool LiberadaDelContextoDelEjecutador; // Del contexto del ejecutador
		public bool RegistroConDatos=false;
		// public SentenciaSelect SentenciaSubSelect;
		// public SelectInterno SelectInterno;
		public Tabla()
		{
			Construir();
			NombreTabla=this.GetType().Name.ToLowerInvariant();
		}
		public Tabla(BaseDatos db,params object[] Claves)
			:this()
		{
			Leer(db,Claves);
		}
		public bool ContieneMismoNombre(Campo c){
			return campos.Exists(
				delegate(Campo contenido){ 
					return contenido.NombreCampo==c.NombreCampo; 
				}
			);
		}
		public Lista<Campo> Campos(){
			return campos;
		}
		public virtual string ToSql(BaseDatos db){
			/*
			return (SelectInterno==null
					?db.StuffTabla(this.NombreTabla)
					:"("+SelectInterno.ToSql(db)+")")
				+(this.Alias==null?"":" "+this.Alias);
			*/
			return db.StuffTabla(this.NombreTabla)+(this.Alias==null?"":" "+this.Alias);
		}
		public ConjuntoTablas Tablas(QueTablas queTablas)
		{
			return new ConjuntoTablas();
		}
		public static string NombreFieldANombreCampo(string nombreField){
			return nombreField.Substring(1);
		}
		protected virtual void ConstruirCampos(){
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				c.Nombre=NombreFieldANombreCampo(m.Name);
      				c.NombreCampo=c.Nombre.ToLowerInvariant();
      				c.TablaContenedora=this;
      				m.SetValue(this,c);
      				foreach (System.Attribute attr in m.GetCustomAttributes(true)){
      					if(attr is AplicadorCampo){
      						AplicadorCampo apl=attr as AplicadorCampo;
      						apl.Aplicar(ref c);
      					}
      					if(attr is Pk){
      						CantidadCamposPk++;
      					}
      					if(attr is Fk){
      						Fk fk=attr as Fk;
      						if(fk.Alias!=null){
      							CamposFkAlias[fk.Alias]=c;
      						}
      					}
      				}
				}
			}
		}
		protected void Construir(){
			ConstruirCampos();
		}
		public string SentenciaCreateTable(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			StringBuilder pk=new StringBuilder("\t"+"primary key (");
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			rta.AppendLine("create table "+this.NombreTabla+"(");
      		Separador comapk=new Separador(",");
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					rta.AppendLine("\t"+c.NombreCampo+" "+c.TipoCampo+c.Opcionalidad+c.DefinicionPorDefecto(db)+",");
					if(c.EsPk){
						pk.Append(comapk+c.NombreCampo);
					}
				}
			}
      		pk.Append(")");
      		rta.Append(pk);
      		UsarFk();
      		if(TablasFk!=null){
	      		foreach(Tabla t in TablasFk){
      				if(t.TipoFk==Fk.Tipo.Obligatoria || t.TipoFk!=Fk.Tipo.Sugerida && db.SoportaFkMixta){
		      			StringBuilder camposFkEsta=new StringBuilder();
		      			StringBuilder camposFkOtra=new StringBuilder();
		      			Separador coma=new Separador(",");
		      			foreach(Campo c in t.CamposPk()){
		      				camposFkEsta.Append(coma+t.CamposRelacionadosFk[c].UnicoCampo().NombreCampo);
		      				camposFkOtra.Append(coma.mismo()+c.NombreCampo);
		      			}
		      			rta.Append(",\n\t"+"foreign key ("+camposFkEsta.ToString()+") references "+t.NombreTabla+" ("+camposFkOtra.ToString()+")");
      				}
	      		}
      		}
			rta.AppendLine("\n);");
			return rta.ToString();
		}
		public virtual void InsertarValores(BaseDatos db,params Campable[] Campos){
			Sentencia s=new SentenciaInsert(this).Valores(Campos);
			Ejecutador ej=new Ejecutador(db);
			ej.Ejecutar(s);
		}
		public virtual Tabla InsertarDirecto(BaseDatos db,params object[] Valores){
			int i=0;
			using(Insertador ins=new Insertador(db,this)){
				foreach(Campo c in this.Campos()){
				if(i>=Valores.Length) break;
					c[ins]=Valores[i];
					i++;
				}
			}
			Leer(db,Valores);
			return this;
		}
		/*
		public void Insertar(BaseDatos db,params object[] Valores){
			using(InsertadorSql ins=new InsertadorSql(db,this.NombreTabla)){
      			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
      			int i=0;
				foreach(FieldInfo m in ms){
				if(i>=Valores.Length) break;
					if(m.FieldType.IsSubclassOf(typeof(Campo))){
						Campo c=(Campo)m.GetValue(this);
						ins[c.Nombre]=Valores[i];
					}
					i++;
      			}
      			// ins.InsertarSiHayCampos();
			}
		}
		*/
		public Insertador Insertar(BaseDatos db){
			return new Insertador(db,this);
		}
		public virtual void Leer(BaseDatos db,params object[] Codigos){
			BuscarYLeer(db,true,Codigos);
		}
		public virtual bool Buscar(BaseDatos db,params object[] Codigos){
			return BuscarYLeer(db,false,Codigos);
		}
		bool BuscarYLeer(BaseDatos db,bool LanzaExcepcion,params object[] Codigos){
			this.db=db;
			List<object> Valores=new List<object>();
			foreach(object o in Codigos){
				if(o is Campo){
					Campo c=o as Campo;
					if(c.ValorSinTipo!=null){
						Valores.Add(c);
					}else if(c is CampoAlias && (c as CampoAlias).ExpresionBase.CandidatoAGroupBy==false){
						Valores.Add((c as CampoAlias).ExpresionBase);
					}else{
						Falla.Detener("BuscarYLeer un parámetro Campo ("+c.Nombre+") no tiene valor ni expresion base");
					}
				}else if(o is Tabla){
					Tabla t=o as Tabla;
					foreach(Campo c in t.CamposPk()){
						Valores.Add(c.ValorSinTipo);
					}
				}else{
					Valores.Add(o);
				}
			}
			int i=0;
			object[] parametros=new object[CantidadCamposPk*2];
			foreach(Campo c in CamposPk()){
			if(i>=Valores.Count) break;
				parametros[i*2]=c.NombreCampo;
				parametros[i*2+1]=Valores[i];
				i++;
			}
			if(i<CamposPk().Count){
				Falla.Detener("Faltaron especificar campos en BuscarYLeer de "+this.NombreTabla+" con "+Objeto.ExpandirMiembros(Codigos));
			}
			/*
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
			if(i>=Valores.Count) break;
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.EsPk){
						parametros[i*2]=c.NombreCampo;
						parametros[i*2+1]=Valores[i];
					}
				}
				i++;
  			}
  			*/
  			return BuscarYLeerNoPk(db,LanzaExcepcion,parametros);
		}
		public virtual void LeerNoPk(BaseDatos db,params object[] parametros){
			BuscarYLeerNoPk(db,true,parametros);
		}
		bool BuscarYLeerNoPk(BaseDatos db,bool LanzaExcepcion,params object[] parametros){
			this.db=db;
			Separador whereAnd=new Separador("\n WHERE ","\n AND ");
			StringBuilder clausulaWhere=new StringBuilder();
			for(int i=0;i<parametros.Length;i++){
				object campo=parametros[i];
				object valor=null;
				if(campo is Campo){
					Campo c=campo as Campo;
					campo=c.NombreCampo;
					if(c is CampoAlias){
						valor=(c as CampoAlias).ExpresionBase.ToSql(db);
					}else{
						valor=db.StuffValor(c.ValorSinTipo);
					}
				}else if(campo is string){
					i++;
					valor=parametros[i];
					if(valor is Campo){
						Campo c=valor as Campo;
						valor=db.StuffValor(c.ValorSinTipo);
					}else{
						valor=db.StuffValor(valor);
					}
				}else{
					Falla.Detener("El parámetro no tiene el tipo string o campo.Es "+Objeto.ExpandirMiembros(campo));
				}
				clausulaWhere.Append(whereAnd+campo+"="+valor);
  			}
			IDataReader SelectAbierto=db.ExecuteReader("SELECT * FROM "+db.StuffTabla(NombreTabla)+clausulaWhere+";");
			RegistroConDatos=SelectAbierto.Read();
			if(RegistroConDatos){
				LevantarCampos(SelectAbierto);
			}else{
				if(LanzaExcepcion){
					throw new SystemException("No existe el campo descripto por "+Objeto.ExpandirMiembros(parametros));
				}
			}
			return RegistroConDatos;
		}
		public void LevantarCampos(IDataReader SelectAbierto){
			RegistroConDatos=true;
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					c.AsignarValor(SelectAbierto[c.NombreCampo]);
				}
  			}
		}
		public virtual ListaElementos<Campo> Campos(Filtro filtro){
			ListaSqlizable<Campo> rta=new ListaSqlizable<Campo>();
			foreach(Campo c in campos){
				if(filtro==null || filtro(c)){
					rta.Add(c);
				}
  			}
  			return rta;
		}
		public virtual ListaElementos<Campo> CamposPk(){
			return Campos(delegate(Campo c){ return c.EsPk; });
		}
		public virtual string OrderBy(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador coma=new Separador(", ");
			foreach(Campo c in CamposPk()){
				rta.Append(coma+c.ToSql(db));
			}
			return rta.ToString();
		}
		public virtual bool TieneElCampo(Campo campo){
			ListaSqlizable<Campo> rta=new ListaSqlizable<Campo>();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.Nombre==campo.Nombre){
						return true;
					}
				}
  			}
  			return false;
		}
		public virtual Campo CampoIndirecto(string campoNombre){
			ListaSqlizable<Campo> rta=new ListaSqlizable<Campo>();
  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					if(c.Nombre==campoNombre){
						return c;
					}
				}
  			}
  			Falla.Detener("Debió encontrar el campo "+campoNombre);
  			return null;
		}
		public virtual Campo CampoIndirecto(Campo campo){
			return CampoIndirecto(campo.Nombre);
		}
		public string MostrarCampos(){
			StringBuilder rta=new StringBuilder();
			Separador coma=new Separador(", ");
			foreach(Campo c in Campos()){
				rta.Append(coma+c.Nombre+"="+Cadena.DelValor(c.ValorSinTipo));
			}
			return rta.ToString();
		}
		public void EsFkDe(Tabla maestra,Fk.Tipo TipoFk,params Campo[] CamposReemplazo){
			this.TablaRelacionada=maestra;
			int cantidadCamposFk=CamposPk().Count;
			ListaSqlizable<Campo> CampoAReemplazar=new ListaSqlizable<Campo>();
			Lista<ExpresionSql> ExpresionDeReemplazo=new Lista<ExpresionSql>();
			CamposRelacionadosFk=new Diccionario<Campo,ExpresionSql>();
			foreach(Campo CampoReemplazo in CamposReemplazo){
				if(CampoReemplazo!=null){
					if(CampoReemplazo.TablaContenedora==maestra){
						CampoAReemplazar.Add(CamposPk()[cantidadCamposFk-1]);
						ExpresionDeReemplazo.Add(new ExpresionSql(CampoReemplazo));
					}else if(CampoReemplazo is CampoAlias){
						CampoAReemplazar.Add((CampoReemplazo as CampoAlias).CampoReceptor);
						ExpresionDeReemplazo.Add((CampoReemplazo as CampoAlias).ExpresionBase);
					}
				}
			}
			foreach(Campo c in CamposPk()){
				if(CampoAReemplazar!=null && CampoAReemplazar.IndexOf(c)>=0){
					CamposRelacionadosFk[c]=ExpresionDeReemplazo[CampoAReemplazar.IndexOf(c)];
				}else{
					CamposRelacionadosFk[c]=new ExpresionSql(maestra.CampoIndirecto(c));
				}
			}
			this.TipoFk=TipoFk;
		}
		public void EsFkDe(Tabla maestra){
			EsFkDe(maestra,Fk.Tipo.Sugerida,new Campo[]{});
		}
		public void EsFkDe(Tabla maestra,params Campo[] CampoReemplazo){
			EsFkDe(maestra,Fk.Tipo.Sugerida,CampoReemplazo);
		}
		public void UsarFk(){
			if(!IniciadasFk || RegistroConDatos){
				TablasFk=new Lista<Tabla>();
      			Assembly assem = Assembly.GetExecutingAssembly();
	  			System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				foreach(FieldInfo m in ms){
					if(m.FieldType.IsSubclassOf(typeof(Tabla))){
	  					foreach (System.Attribute attr in m.GetCustomAttributes(true)){
	  						if(attr is Fk){
	  							Fk fk=attr as Fk;
	  							if(!IniciadasFk){
	      							Tabla nueva=(Tabla)assem.CreateInstance(m.FieldType.FullName);
	      							nueva.EsFkDe(this,fk.TipoFk,(fk.Alias!=null?this.CamposFkAlias[fk.Alias]:null));
	      							m.SetValue(this,nueva);
	      							TablasFk.Add(nueva);
	  							}
	  							if(RegistroConDatos){
	  								Tabla fkTabla=(Tabla) this.GetType().GetField(m.Name).GetValue(this);
	  								Lista<object> Campos=new Lista<object>();
	  								foreach(Campo c in fkTabla.CamposPk()){
	  									Campos.Add((fkTabla.CamposRelacionadosFk[c]).UnicoCampo().ValorSinTipo);
	  								}
	  								fkTabla.Buscar(db,Campos.ToArray());
	  							}
	  						}
	  					}
	  				}
	  			}
	  			IniciadasFk=true;
			}
		}
		public override bool CandidatoAGroupBy{ 
			get{ // No tiene en el sentido de que no es una expresión
				Falla.Detener("No debería preguntar si una tabla tiene variables");
				return false; 
			} 
		} 
		/*
		public ExpresionSql SelectSuma(Campo CampoSumar,ExpresionSql ExpresionWhere){
			return new ExpresionSql.SelectSuma(this,CampoSumar,ExpresionWhere);
		}
		*/
		public ExpresionSql SelectSuma(Campo CampoSumar){
			return new ExpresionSql.SelectAgrupado(this,CampoSumar,null,"SUM","","");
		}
		public ExpresionSql SelectPromedioGeometrico(Campo CampoSumar){
			return new ExpresionSql.SelectAgrupado(this,CampoSumar,null,"AVG","","");
			// return new ExpresionSql.SelectAgrupado(this,CampoSumar,null,"AVG","EXP(","LOG(");
		}
		public RegistrosEnumerables Todos(BaseDatos db){
			return new RegistrosEnumerables(this,db);
		}
		public RegistrosEnumerables Algunos(BaseDatos db,ExpresionSql ExpresionWhere,params Campo[] CamposOrden){
			return new RegistrosEnumerables(this,db,ExpresionWhere,CamposOrden);
		}
		public SentenciaSelect SubSelect(params Campo[] Campos){
			SentenciaSubSelect=new SentenciaSelect(Campos);
			SelectInterno=new SelectInterno(SentenciaSubSelect);
			return SentenciaSubSelect;
		}
		public ExpresionSql NoExistePara(params Campable[] CamposOTablas){
			ListaSqlizable<Campo> Campos=new ListaSqlizable<Campo>();
			ListaSqlizable<ExpresionSql> ExpresionesWhere=new ListaSqlizable<ExpresionSql>();
			ConjuntoTablas TablasLibres=new ConjuntoTablas();
			foreach(Campable cc in CamposOTablas){
				foreach(Campo c in cc.Campos()){
					if(this.TieneElCampo(c) && this.CampoIndirecto(c).EsPk){
						ExpresionesWhere.Add(this.CampoIndirecto(c).Igual(c));
						if(c.TablaContenedora!=this){
							TablasLibres.Add(c.TablaContenedora);
						}
					}
				}
			}
			Sentencia s=new SentenciaSelect(this.CamposPk()[0])
				.Where(ExpresionesWhere.ToArray())
				.Libres(TablasLibres);
			return new ExpresionSql(new LiteralSql("NOT EXISTS ("),new SelectInterno(s),new LiteralSql(")"));
		}
		/*
		public ExpresionSql NotIn(Tabla t){
			ListaSqlizable<Sqlizable> Partes1=new ListaSqlizable<Sqlizable>();
			ListaSqlizable<Sqlizable> Partes2=new ListaSqlizable<Sqlizable>();
			ParteSeparadora p1=new ParteSeparadora("(",", ");
			ParteSeparadora p2=new ParteSeparadora(") NOT IN (SELECT ",",");
			foreach(Campo c in this.CamposPk()){
				if(t.TieneElCampo(c)){
					p1.AgregarEn(Partes1,c);
					p2.AgregarEn(Partes2,t.CampoIndirecto(c));
				}
			}
			if(Partes1.Count==0){
				Falla.Detener("Tiene que haber campos en el not in");
			}
			Partes1.AddRange(Partes2);
			Partes1.Add(new LiteralSql(" FROM "));
			Partes1.Add(new LiteralSql(t.NombreTabla));
			Partes1.Add(new LiteralSql(")"));
			return new ExpresionSql(Partes1);
		}
		*/
	}
	public class RegistrosEnumerables{
		BaseDatos db;
		Tabla TablaARecorrer;
		ExpresionSql ExpresionWhere;
		Campo[] CamposOrden;
		public RegistrosEnumerables(Tabla TablaARecorrer,BaseDatos db)
			:this(TablaARecorrer,db,null,new Campo[]{})
		{}
		public RegistrosEnumerables(Tabla TablaARecorrer,BaseDatos db,ExpresionSql ExpresionWhere,Campo[] CamposOrden){
			this.db=db;
			this.TablaARecorrer=TablaARecorrer;
			this.ExpresionWhere=ExpresionWhere;
			this.CamposOrden=CamposOrden;
		}
		public IteradorRegistro GetEnumerator(){
			return new IteradorRegistro(TablaARecorrer,db,ExpresionWhere,CamposOrden);
		}
	}
	public class IteradorRegistro{
		Tabla RegistroActual;
		IDataReader SelectAbierto;
		BaseDatos db;
		bool HayActual;
		string Sentencia;
		public IteradorRegistro(Tabla TablaBase,BaseDatos db,ExpresionSql expresionWhere,params Campo[] Campos){
			this.db=db;
			this.RegistroActual=TablaBase;
			StringBuilder s=new StringBuilder();
			s.Append("SELECT * FROM "+db.StuffTabla(TablaBase.NombreTabla));
			if(expresionWhere!=null){
				s.Append(" WHERE "+expresionWhere.ToSql(db));
			}
			if(Campos.Length==0){
				s.Append(" ORDER BY "+TablaBase.OrderBy(db));
			}else{
				Separador coma=new Separador(" ORDER BY ",", ");
				foreach(Campo c in Campos){
					s.Append(coma+c.ToSql(db)+c.DireccionOrderBy);
				}
			}
			s.Append(";");
			Sentencia=s.ToString();
			Reset();
		}
		public void Reset(){
			SelectAbierto=db.ExecuteReader(Sentencia);
			HayActual=false;
		}
		public bool MoveNext(){
			HayActual=SelectAbierto.Read();
			if(HayActual){
				RegistroActual.LevantarCampos(SelectAbierto);
			}
			return HayActual;
		}
		public Tabla Current{
			get{
				if(HayActual){
					return RegistroActual; 
				}else{
					throw new InvalidOperationException();
				}
			}
		}
		public void Dispose(){
			SelectAbierto.Close();
		}
	}
	/////////////
	public class Vista:System.Attribute{}
	public class Fk:System.Attribute{		
		public enum Tipo { Obligatoria, Mixta/*puede tener algún campo null y otro no*/, Sugerida/*solo para los joins*/ };
		public Tipo TipoFk;
		public string Alias;
		public Fk(){}
		public Fk(string Alias):this(Alias,Tipo.Obligatoria){}
		protected Fk(string Alias,Tipo TipoFk){
			this.Alias=Alias;
			this.TipoFk=TipoFk;
		}
	}
	public class FkMixta:Fk{
		public FkMixta(string Alias)
			:base(Alias,Tipo.Mixta)
		{
		}
	}
	public class Insertador:InsertadorSql{
		public Insertador(BaseDatos db,Tabla tabla)
			:base(db,tabla.NombreTabla)
		{}
	}
	public abstract class Sqlizable{
		public abstract string ToSql(BaseDatos db);
		public abstract ConjuntoTablas Tablas(QueTablas queTablas);
		public abstract bool CandidatoAGroupBy{ get; }
	}
	public abstract class Campable:Sqlizable{
		public abstract ListaSqlizable<Campo> Campos();
	}
	public class LiteralSql:Sqlizable{
		public string Literal;
		public LiteralSql(string Literal){
			this.Literal=Literal;
		}
		public override string ToSql(BaseDatos db){
			return Literal;
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
	}
	public abstract class OperadorDependienteDeLaBase:Sqlizable{
		public override bool CandidatoAGroupBy{ get{return false;} }
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
	}
	public class OperadorConcatenacionIzquierda:OperadorDependienteDeLaBase{
		public override string ToSql(BaseDatos db){
			return db.OperadorConcatenacionIzquierda;
		}
	}
	public class OperadorConcatenacionDerecha:OperadorDependienteDeLaBase{
		public override string ToSql(BaseDatos db){
			return db.OperadorConcatenacionDerecha;
		}
	}
	public class OperadorConcatenacionMedio:OperadorDependienteDeLaBase{
		public override string ToSql(BaseDatos db){
			return db.OperadorConcatenacionMedio;
		}
	}
	public class FuncionLn:OperadorDependienteDeLaBase{
		public override string ToSql(BaseDatos db){
			return db.FuncionLn;
		}
	}
	public class ValorSql<T>:Sqlizable{
		public T Valor;
		public ValorSql(T Valor){
			this.Valor=Valor;
		}
		public override string ToSql(BaseDatos db){
			if(Valor is Sqlizable){
				Sqlizable s=Valor as Sqlizable;
				return s.ToSql(db);
			}
			return db.StuffValor(Valor);
		}
		public override bool CandidatoAGroupBy{ 
			get{
				if(Valor is Sqlizable){
					return (Valor as Sqlizable).CandidatoAGroupBy;
				}
				return false;
			}
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			if(Valor is Sqlizable){
				return (Valor as Sqlizable).Tablas(queTablas);
			}
			return new ConjuntoTablas();
		}
	}
	public class ValorSqlNulo:Sqlizable{
		public ValorSqlNulo(){
		}
		public override string ToSql(BaseDatos db){
			return "null";
		}
		public override bool CandidatoAGroupBy{ get{return false;} }
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
	}
}
